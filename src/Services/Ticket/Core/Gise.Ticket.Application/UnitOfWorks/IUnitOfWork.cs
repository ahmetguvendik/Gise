namespace Gise.Ticket.Application.UnitOfWorks;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
