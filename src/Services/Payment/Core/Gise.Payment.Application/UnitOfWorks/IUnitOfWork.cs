namespace Gise.Payment.Application.UnitOfWorks;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
