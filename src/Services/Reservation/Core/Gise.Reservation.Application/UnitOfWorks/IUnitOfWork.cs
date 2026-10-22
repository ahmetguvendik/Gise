namespace Gise.Reservation.Application.UnitOfWorks;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
