using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gise.Notification.Persistence.Contexts;

// Yalnızca `dotnet ef` için: migration üretirken veritabanına bağlanılmaz, Api'nin ayağa kalkması gerekmez.
public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseNpgsql("Host=localhost;Database=gise_notification")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new NotificationDbContext(options);
    }
}
