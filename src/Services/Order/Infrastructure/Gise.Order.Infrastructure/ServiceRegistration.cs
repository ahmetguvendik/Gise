using Gise.Order.Application.Sagas;
using Gise.Order.Persistence.Contexts;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Gise.Order.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Order")
            ?? throw new InvalidOperationException("ConnectionStrings:Order ayarı eksik.");

        // Saga'nın zaman aşımları (ödeme süresi) Quartz ile Order veritabanında saklanır; order-api yeniden başlasa da
        // kaybolmaz. Tabloları (qrtz_*) Order'ın migration'ları kurar.
        services.AddQuartz(quartz =>
        {
            quartz.SchedulerName = "gise-order-scheduler";
            // Kümede her kopya ayrı kimlik alır; aynı kimlik, çöken bir kopyanın tetiklenmemiş işlerinin fark edilmemesine yol açar.
            quartz.SchedulerId = "AUTO";
            quartz.UsePersistentStore(store =>
            {
                store.UseProperties = true;
                store.UsePostgres(connectionString);
                store.UseSystemTextJsonSerializer();
                // order-api birden çok kopya çalışırsa planlanan mesajı yalnızca biri tetikler.
                store.UseClustering();
            });
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        // Satın alma saga'sı (RabbitMQ). Ayarlar appsettings'teki RabbitMq bölümünden okunur.
        var rabbit = configuration.GetRequiredSection("RabbitMq");
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            // Inbox: aynı mesaj iki kez gelirse ikincisi işlenmez. Outbox: saga'nın gönderdiği komutlar saga durumuyla,
            // API'nin yayımladığı mesajlar (OrderSubmitted…) siparişle aynı transaction'da yazılır, sonra gönderilir.
            x.AddEntityFrameworkOutbox<OrderDbContext>(outbox =>
            {
                outbox.UsePostgres();
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

        return services;
    }
}
