using Elastic.CommonSchema.Serilog;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Extensions.Hosting;

namespace Gise.Logging;

public static class LoggingExtensions
{
    // Loglar ECS biçiminde konsola ve Kafka'ya yazılır: API'ler logs.api'ye, worker'lar logs.workers'a, insanların yaptığı
    // işlemler logs.audit'e; Logstash her birini kendi akışına taşır (bkz. tasarım 8). Konsol her zaman açık: Kafka,
    // Logstash ya da Elasticsearch çökse de "docker compose logs" çalışır. Seviyeler appsettings'teki Serilog bölümünden,
    // Kafka ayarları Logging:Kafka bölümünden okunur.
    public static void UseAppLogging(this IHostApplicationBuilder builder, string serviceName)
    {
        // Kullanıcı kimliği istekten okunur; accessor ancak bu kayıtla doldurulur.
        builder.Services.AddHttpContextAccessor();

        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.With<TraceEnricher>()
            .Enrich.With(new UserEnricher(new HttpContextAccessor()))
            .Enrich.WithProperty("service.name", serviceName)
            .Enrich.WithProperty("service.environment", builder.Environment.EnvironmentName)
            .WriteTo.Console(new EcsTextFormatter());

        AddKafkaSink(logger, builder.Configuration);

        Log.Logger = logger.CreateLogger();

        builder.Logging.ClearProviders();
        // dispose: kapanışta kuyrukta bekleyen loglar da Kafka'ya gönderilsin.
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        // İstek logu (UseAppRequestLogging) satırına alan eklemek için bunu ister.
        var diagnosticContext = new DiagnosticContext(Log.Logger);
        builder.Services.AddSingleton(diagnosticContext);
        builder.Services.AddSingleton<IDiagnosticContext>(diagnosticContext);
    }

    // Kafka ayarı yoksa sink kurulmaz; Kafka'sız çalışılabilsin.
    private static void AddKafkaSink(LoggerConfiguration logger, IConfiguration configuration)
    {
        var kafka = configuration.GetSection("Logging:Kafka");
        var bootstrapServers = kafka["BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrapServers))
            return;

        // API'ler logs.api'ye, worker'lar logs.workers'a (appsettings); unutulan bir process'in logları kaybolmaz, API
        // akışında görünür.
        var sink = new KafkaLogSink(
            bootstrapServers,
            kafka["Topic"] ?? "logs.api",
            kafka["AuditTopic"] ?? "logs.audit",
            kafka.GetValue("Partitions", 3),
            TimeSpan.FromHours(kafka.GetValue("RetentionHours", 24)));

        logger.WriteTo.Sink(sink, new BatchingOptions
        {
            BatchSizeLimit = kafka.GetValue("BatchSize", 500),
            BufferingTimeLimit = TimeSpan.FromSeconds(kafka.GetValue("FlushSeconds", 2)),
            // Kuyruk dolunca yeni olaylar düşürülür: log kaybı, isteklerin yavaşlamasından iyidir.
            QueueLimit = kafka.GetValue("QueueLimit", 10_000),
            EagerlyEmitFirstEvent = false
        });
    }
}
