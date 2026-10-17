namespace Gise.Catalog.Application.Exceptions;

// Rol yeterli ama kayıt kullanıcıya ait değil (ör. başka organizatörün etkinliği).
public sealed class ForbiddenException(string message) : Exception(message);
