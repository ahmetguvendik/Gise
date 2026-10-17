using Gise.Search.Persistence;
using Gise.Search.Persistence.Contexts;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistenceServices(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<SearchDbContext>("postgres", tags: ["ready"]);

var app = builder.Build();

// Şema açılışta migration'larla güncellenir. Aynı anda açılan kopyalar için EF Core
// veritabanında kilit alır; migration'lar sırayla ve bir kez uygulanır.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<SearchDbContext>().Database.MigrateAsync();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
