using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gise.Order.Persistence.Contexts;

// Yalnızca `dotnet ef` için: migration üretirken veritabanına bağlanılmaz, Api'nin ayağa kalkması gerekmez.
public sealed class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql("Host=localhost;Database=gise_order")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new OrderDbContext(options);
    }
}
