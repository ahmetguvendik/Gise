using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Application.UnitOfWorks;
using Gise.Ticket.Persistence.Contexts;
using Gise.Ticket.Persistence.Repositories;
using Gise.Ticket.Persistence.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Ticket.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TicketDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("Ticket"))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ICancelledEventRepository, CancelledEventRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
