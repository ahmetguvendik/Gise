namespace Gise.Reservation.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    // DbContext kayıt sırasında doldurur; entity'nin kendisi set etmez.
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}
