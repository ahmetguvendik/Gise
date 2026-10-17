using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gise.Reservation.Persistence.Contexts;

// Yalnızca `dotnet ef` için: migration üretirken veritabanına bağlanılmaz, Api'nin ayağa kalkması gerekmez.
public sealed class ReservationDbContextFactory : IDesignTimeDbContextFactory<ReservationDbContext>
{
    public ReservationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReservationDbContext>()
            .UseNpgsql("Host=localhost;Database=gise_reservation")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ReservationDbContext(options);
    }
}
