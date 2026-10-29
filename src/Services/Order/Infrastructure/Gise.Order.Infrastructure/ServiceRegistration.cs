using Gise.Contracts.Grpc.Catalog;
using Gise.Order.Application.Interfaces;
using Gise.Order.Infrastructure.Grpc;
using Gise.Order.Persistence.Contexts;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Order.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Catalog'un gRPC ucu: yalnızca Docker ağının içinden, 8081 (HTTP/2, TLS'siz).
        var catalogGrpcUrl = configuration.GetRequiredSection("Catalog")["GrpcUrl"]
            ?? throw new InvalidOperationException("Catalog:GrpcUrl ayarı eksik.");
        services.AddGrpcClient<CatalogGrpc.CatalogGrpcClient>(options => options.Address = new Uri(catalogGrpcUrl));
        services.AddScoped<ICatalogClient, CatalogClient>();

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
