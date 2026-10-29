namespace Gise.Notification.Application.Interfaces;

public interface IEmailSender
{
    // Gövde HTML. Geçici hata (SMTP'ye ulaşılamadı) exception fırlatır; mesaj yeniden denenir.
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken);
}
