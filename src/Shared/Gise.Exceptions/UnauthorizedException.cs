namespace Gise.Exceptions;

// Kimlik bilgisi eksik ya da geçersiz. → 401
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string errorCode, string message, string description)
        : base(401, errorCode, message, description)
    {
    }

    public static UnauthorizedException MissingClaim(string claimName) => new(
        ErrorCodes.Auth.MissingClaim,
        "Kimlik doğrulama bilgisi eksik.",
        $"Token içinde '{claimName}' claim'i yok. Yeniden giriş yapıp tekrar deneyin.");
}
