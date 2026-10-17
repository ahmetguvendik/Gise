using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gise.Ticket.Persistence.Contexts;

// Yalnızca `dotnet ef` için: migration üretirken veritabanına bağlanılmaz, Api'nin ayağa kalkması gerekmez.
public sealed class TicketDbContextFactory : IDesignTimeDbContextFactory<TicketDbContext>
{
    public TicketDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TicketDbContext>()
            .UseNpgsql("Host=localhost;Database=gise_ticket")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new TicketDbContext(options);
    }
}
