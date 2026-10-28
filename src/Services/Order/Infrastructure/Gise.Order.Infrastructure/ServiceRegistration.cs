using Gise.Order.Persistence.Contexts;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Order.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // order-api mesaj göndermez ve dinlemez: yayımladığı mesajlar (OrderSubmitted…) siparişle aynı transaction'da
        // outbox'a yazılır, RabbitMQ'ya order-saga-worker gönderir. RabbitMQ'ya yine bağlanır; outbox'a yazılan
        // hedef adresleri RabbitMQ'nunkiler olsun.
        var rabbit = configuration.GetRequiredSection("RabbitMq");
        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<OrderDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox(bus => bus.DisableDeliveryService());
            });

            x.UsingRabbitMq((_, bus) => bus.Host(rabbit["Host"], "/", host =>
            {
                host.Username(rabbit["Username"] ?? throw new InvalidOperationException("RabbitMq:Username ayarı eksik."));
                host.Password(rabbit["Password"] ?? throw new InvalidOperationException("RabbitMq:Password ayarı eksik."));
            }));
        });

        return services;
    }
}
