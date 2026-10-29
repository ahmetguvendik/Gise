using System.Net;
using System.Net.Mail;
using Gise.Notification.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Gise.Notification.Infrastructure.Email;

// Telgraf'taki gibi System.Net.Mail ile SMTP (Gmail). Kullanıcı adı ve parola compose'dan ortam değişkeniyle gelir
// (deploy/.env: SMTP_USERNAME, SMTP_PASSWORD; Keycloak'ın kullandığı hesap). Ayarlar appsettings'teki Smtp bölümünden.
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var smtp = _configuration.GetRequiredSection("Smtp");
        var username = smtp["Username"];
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("Smtp:Username ayarı eksik; e-posta gönderilemiyor.");

        using var client = new SmtpClient(smtp["Host"], smtp.GetValue<int>("Port"))
        {
            Credentials = new NetworkCredential(username, smtp["Password"]),
            // Gmail STARTTLS ister; yerel bir test sunucusu için kapatılabilir.
            EnableSsl = smtp.GetValue("EnableSsl", true)
        };

        // Gmail gönderen olarak yalnızca oturum açan hesabı kabul eder.
        using var message = new MailMessage
        {
            From = new MailAddress(username, smtp["FromName"]),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message, cancellationToken);
    }
}
