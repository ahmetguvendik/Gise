using Gise.Search.Persistence;
using Gise.Search.Persistence.Contexts;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistenceServices(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<SearchDbContext>("postgres", tags: ["ready"]);

var app = builder.Build();

// Migration'lar servis açılırken değil, dağıtımda ayrı bir adımda uygulanır:
//   dotnet Gise.Search.Api.dll migrate
if (args.Contains("migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<SearchDbContext>().Database.MigrateAsync();
    return;
}

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
