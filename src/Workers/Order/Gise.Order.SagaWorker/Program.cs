using Gise.Order.Application.Sagas;
using Gise.Order.Persistence;
using Gise.Order.Persistence.Contexts;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Saga'nın güncellediği siparişler (IOrderRepository) ve saga durumu aynı OrderDbContext'te. Tabloları order-api'nin
// migration'ları kurar; worker migration çalıştırmaz.
builder.Services.AddPersistenceServices(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("Order")
    ?? throw new InvalidOperationException("ConnectionStrings:Order ayarı eksik.");

// Saga'nın zaman aşımları (ödeme süresi) Quartz ile Order veritabanında (qrtz_*) saklanır; worker yeniden başlasa da
// kaybolmaz.
builder.Services.AddQuartz(quartz =>
{
    quartz.SchedulerName = "gise-order-scheduler";
    // Kümede her kopya ayrı kimlik alır; aynı kimlik, çöken bir kopyanın tetiklenmemiş işlerinin fark edilmemesine yol açar.
    quartz.SchedulerId = "AUTO";
    quartz.UsePersistentStore(store =>
    {
        store.UseProperties = true;
        store.UsePostgres(connectionString);
        store.UseSystemTextJsonSerializer();
        // Worker birden çok kopya çalışırsa planlanan mesajı yalnızca biri tetikler.
        store.UseClustering();
    });
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// Ayarlar appsettings'teki RabbitMq ve Outbox bölümlerinden okunur.
var rabbit = builder.Configuration.GetRequiredSection("RabbitMq");
// order-api ayrı process olduğu için yazdığı mesajdan haberdar olunmaz; outbox bu aralıkla taranır.
var outboxQueryDelay = TimeSpan.FromSeconds(builder.Configuration.GetRequiredSection("Outbox").GetValue<int>("QueryDelaySeconds"));
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    // Inbox: aynı mesaj iki kez gelirse ikincisi işlenmez. Outbox: saga'nın gönderdiği komutlar saga durumuyla aynı
    // transaction'da yazılır. Gönderim de burada: order-api'nin outbox'a yazdıklarını (OrderSubmitted…) bu worker yollar.
    x.AddEntityFrameworkOutbox<OrderDbContext>(outbox =>
    {
        outbox.UsePostgres();
        outbox.QueryDelay = outboxQueryDelay;
        outbox.UseBusOutbox();
    });

    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(repository =>
        {
            // Sipariş de aynı DbContext'te güncellenir; saga ile birlikte kaydedilir.
            repository.ExistingDbContext<OrderDbContext>();
            repository.UsePostgres();
        });

    // Saga'nın Schedule/Unschedule'ı Quartz'a gider.
    x.AddPublishMessageScheduler();
    x.AddQuartzConsumers();

    x.AddConfigureEndpointsCallback((context, _, endpoint) =>
    {
        // Geçici hatalar (veritabanı) birkaç kez denenir; olmazsa mesaj _error kuyruğuna düşer.
        endpoint.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)));
        endpoint.UseEntityFrameworkOutbox<OrderDbContext>(context);
    });

    x.UsingRabbitMq((context, bus) =>
    {
        bus.Host(rabbit["Host"], "/", host =>
        {
            host.Username(rabbit["Username"] ?? throw new InvalidOperationException("RabbitMq:Username ayarı eksik."));
            host.Password(rabbit["Password"] ?? throw new InvalidOperationException("RabbitMq:Password ayarı eksik."));
        });
        bus.UsePublishMessageScheduler();
        bus.ConfigureEndpoints(context);
    });
});

// MassTransit kendi kontrolünü ("ready" etiketiyle) ekler: RabbitMQ'ya bağlı değilse worker hazır sayılmaz.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>("postgres", tags: ["ready"]);

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
