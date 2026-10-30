using Gise.Contracts.Commands;
using Gise.Payment.Application;
using Gise.Payment.CommandWorker.Messaging;
using Gise.Payment.Infrastructure;
using Gise.Payment.Persistence;
using Gise.Payment.Persistence.Contexts;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Tüketiciler MediatR komutlarını (ProcessPaymentCommand, RefundPaymentCommand) çağırır. Tabloları payment-api'nin
// migration'ları kurar; worker migration çalıştırmaz.
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices();

// Ayarlar appsettings'teki RabbitMq bölümünden okunur.
var rabbit = builder.Configuration.GetRequiredSection("RabbitMq");
builder.Services.AddMassTransit(x =>
{
    // Inbox: aynı komut iki kez gelirse ikincisi işlenmez. Outbox: cevap (PaymentSucceeded…) ödeme kaydıyla aynı
    // transaction'da yazılır, sonra gönderilir.
    x.AddEntityFrameworkOutbox<PaymentDbContext>(outbox => outbox.UsePostgres());

    x.AddConsumer<ProcessPaymentConsumer>().Endpoint(e => e.Name = Queues.ProcessPayment);
    x.AddConsumer<RefundPaymentConsumer>().Endpoint(e => e.Name = Queues.RefundPayment);

    x.AddConfigureEndpointsCallback((context, _, endpoint) =>
    {
        // Geçici hatalar (sağlayıcı, veritabanı, aynı siparişin ödemesi ile iadesinin çakışması) birkaç kez denenir;
        // olmazsa mesaj _error kuyruğuna düşer.
        endpoint.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)));
        endpoint.UseEntityFrameworkOutbox<PaymentDbContext>(context);
    });

    x.UsingRabbitMq((context, bus) =>
    {
        bus.Host(rabbit["Host"], "/", host =>
        {
            host.Username(rabbit["Username"] ?? throw new InvalidOperationException("RabbitMq:Username ayarı eksik."));
            host.Password(rabbit["Password"] ?? throw new InvalidOperationException("RabbitMq:Password ayarı eksik."));
        });
        bus.ConfigureEndpoints(context);
    });
});

// MassTransit kendi kontrolünü ("ready" etiketiyle) ekler: RabbitMQ'ya bağlı değilse worker hazır sayılmaz.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentDbContext>("postgres", tags: ["ready"]);

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
