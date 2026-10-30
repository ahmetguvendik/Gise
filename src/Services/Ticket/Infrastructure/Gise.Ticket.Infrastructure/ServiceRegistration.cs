using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Infrastructure.Documents;
using Gise.Ticket.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Ticket.Infrastructure;

public static class ServiceRegistration
{
    // PDF: ticket-command-worker bilet çıkarırken, ticket-api aktarım kabul edilince (yeni QR), ticket-inbox-worker etkinlik
    // güncellenince üretir ve yükler. Üçü de yazı tipini ve TicketPdf ayarını ister.
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<ITicketStorage, S3TicketStorage>();
        // Yazı tiplerini açılışta bir kez yükler; ilk kullanıldığında oluşur.
        services.AddSingleton<ITicketPdfRenderer, TicketPdfRenderer>();

        return services;
    }
}
