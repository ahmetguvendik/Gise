using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Infrastructure.Documents;
using Gise.Ticket.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Ticket.Infrastructure;

public static class ServiceRegistration
{
    // PDF: ticket-command-worker bilet çıkarırken üretir ve yükler; ticket-api yalnızca indirme adresi üretir.
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<ITicketStorage, S3TicketStorage>();
        // Yazı tiplerini açılışta bir kez yükler; ilk kullanıldığında oluşur.
        services.AddSingleton<ITicketPdfRenderer, TicketPdfRenderer>();

        return services;
    }
}
