namespace Gise.Notification.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    // DbContext kayıt sırasında doldurur.
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
