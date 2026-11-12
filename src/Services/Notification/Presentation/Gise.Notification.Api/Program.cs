using Gise.HealthChecks;
using Gise.Logging;
using Gise.Notification.Persistence;
using Gise.Notification.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Loglar Kafka üzerinden Elasticsearch'e, izler Elastic APM'e (bkz. tasarım 8).
builder.UseAppLogging("notification-api");
builder.UseAppApm("notification-api");

builder.Services.AddPersistenceServices(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<NotificationDbContext>("postgres", tags: ["ready"]);

var app = builder.Build();

// Şema açılışta migration'larla güncellenir. Aynı anda açılan kopyalar için EF Core
// veritabanında kilit alır; migration'lar sırayla ve bir kez uygulanır.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.MigrateAsync();
}

// Her istek için tek satır (bkz. tasarım 8); en başta: hata yanıtları da yazılır.
app.UseAppRequestLogging();

app.MapAppHealthChecks();

app.Run();
