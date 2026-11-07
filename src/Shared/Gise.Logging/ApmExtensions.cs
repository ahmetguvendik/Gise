using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gise.Logging;

public static class ApmExtensions
{
    // Ajanın OpenTelemetry köprüsü Activity'lere "örneklendi" (Recorded) bayrağını hiç koymaz; bayrak yalnızca üst Activity'den
    // miras kalır. Saga'nın kendi başlattığı gönderimlerde miras kalacak bayrak olmadığından MassTransit mesaja "örneklenmedi"
    // (traceparent ...-00) yazar ve komutu tüketen worker'ın ajanı işlemi kaydetmez. Bir Activity'nin örnekleme kararı
    // dinleyicilerin en yükseğidir; bu dinleyici her MassTransit Activity'sini örneklenmiş yapar. Ajan zaten her işlemi
    // örnekliyor (TransactionSampleRate 1); oran düşürülürse bu dinleyici yeniden düşünülmeli.
    private static readonly ActivityListener MassTransitSampler = new()
    {
        ShouldListenTo = source => source.Name == "MassTransit",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
    };

    private static int _samplerRegistered;

    // Elastic APM ajanı: istekler, veritabanı ve HTTP/gRPC çağrıları kendiliğinden izlenir (bkz. tasarım 8). Varsayılanlar en
    // düşük öncelikle eklenir; appsettings ya da ortam değişkeni (ElasticApm__...) ezebilir.
    public static void UseAppApm(this IHostApplicationBuilder builder, string serviceName)
    {
        // Testler aynı process'te birden çok host kurabilir; dinleyici bir kez eklenir.
        if (Interlocked.Exchange(ref _samplerRegistered, 1) == 0)
            ActivitySource.AddActivityListener(MassTransitSampler);

        builder.Configuration.Sources.Insert(0, new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["ElasticApm:ServiceName"] = serviceName,
                ["ElasticApm:ServerUrl"] = "http://apm-server:8200",
                ["ElasticApm:Environment"] = builder.Environment.EnvironmentName,

                // Bulut ortamı yok; ajan açılışta AWS, Azure ve GCP meta veri uçlarını yoklamasın.
                ["ElasticApm:CloudProvider"] = "none",

                // Sağlık yoklamaları her birkaç saniyede bir geliyor; kaydedilseler işlemlerin çoğunu oluştururlardı.
                ["ElasticApm:TransactionIgnoreUrls"] = "/health*",

                // Npgsql'in kendi izleri EF Core span'lerinin kopyası; üst işlem yokken (worker yoklamaları, sağlık
                // kontrolleri) her sorgu ayrı işlem açıyor ve APM verisinin %95'ini oluşturuyordu. EF Core sorguları ajanın
                // kendi EF Core izlemesiyle görünmeye devam eder; yalnızca EF Core dışındaki sorgular (Quartz) görünmez.
                ["ElasticApm:OpenTelemetryBridgeDeniedActivitySources"] = "Npgsql",

                // Ajanın varsayılan listesi + *cookie*: Cookie başlığı açık metin kaydediliyordu (Telgraf dersi). Liste
                // değiştirilirse varsayılanlar da yazılmalı, yoksa kaybolurlar.
                ["ElasticApm:SanitizeFieldNames"] =
                    "password, passwd, pwd, secret, *key, *token*, *session*, *credit*, *card*, *auth*, *cookie*, *principal*"
            }
        });

        builder.Services.AddAllElasticApm();
    }
}
