namespace Gise.Exceptions;

// İstek doğrulama kurallarına uymuyor. → 400
public class ValidationException : AppException
{
    // Alan yolu → hata mesajları, ör. "sections[0].rows[1].seatCount".
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(
            400,
            ErrorCodes.General.ValidationFailed,
            "Bazı bilgiler hatalı.",
            "Aşağıdaki alanları düzeltip tekrar deneyin.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }

    public ValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] })
    {
    }
}
