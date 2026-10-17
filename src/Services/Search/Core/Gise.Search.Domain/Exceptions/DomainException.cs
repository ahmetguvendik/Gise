namespace Gise.Search.Domain.Exceptions;

// İş kuralı ihlali. Girdi doğrulaması Application'daki validator'larda; bu, onları atlayan
// bir çağrıya karşı son savunma hattı.
public sealed class DomainException(string message) : Exception(message);
