namespace Gise.Ticket.Application.Interfaces;

// Bilet PDF'leri MinIO'da, herkese açık olmayan tickets/ altında; indirme kısa süreli imzalı adresle.
public interface ITicketStorage
{
    Task UploadPdfAsync(string key, byte[] content, CancellationToken cancellationToken);

    // Silinemezse loglanır, hata fırlatmaz: çağıran işi (ör. aktarım) bozulmasın.
    Task TryDeletePdfAsync(string key, CancellationToken cancellationToken);

    // Tarayıcı dosyayı fileName adıyla kaydeder.
    TicketDownload CreateDownloadUrl(string key, string fileName);
}

public sealed record TicketDownload(string Url, int ExpiresInSeconds);
