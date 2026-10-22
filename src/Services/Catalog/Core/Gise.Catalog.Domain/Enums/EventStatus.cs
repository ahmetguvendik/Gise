namespace Gise.Catalog.Domain.Enums;

public enum EventStatus
{
    Draft,
    // Organizatör onaya gönderdi; admin yayınlar ya da sebebiyle taslağa geri gönderir.
    PendingReview,
    Published,
    Cancelled
}
