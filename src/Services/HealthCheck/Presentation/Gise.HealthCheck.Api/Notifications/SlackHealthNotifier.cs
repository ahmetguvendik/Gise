using System.Text;
using System.Text.Json;
using HealthChecks.UI.Core;
using HealthChecks.UI.Core.Notifications;

namespace Gise.HealthCheck.Api.Notifications;

// Düştü ve düzeldi bildirimi (Slack Incoming Webhook). HealthChecks.UI'nin kendi webhook bildirimcisinin yerine geçer: onun
// [[DESCRIPTIONS]] yer tutucusu kontrolün nedenini değil sabit İngilizce bir kalıbı yazıyor ("redis healthcheck is failing");
// nöbetçinin ihtiyacı olan neden ("rabbitmq:5672 bağlantı kurulamadı", "Son 5 dakikada 21 hata") mesajda olsun. Ne zaman
// bildirileceğine (Unhealthy'ye geçiş ve düzelme) kütüphane karar vermeye devam eder. Ayarlar appsettings'teki Alerting
// bölümünden; webhook adresi gizli bilgi, compose'da deploy/.env'den (HEALTH_WEBHOOK_URL) gelir, boşsa bildirim gitmez.
internal sealed class SlackHealthNotifier : IHealthCheckFailureNotifier
{
    public const string HttpClientName = "slack";

    // Mesaj okunabilir kalsın: çok sayıda kalem ya da uzun hata metni Slack'te duvar olur.
    private const int MaxFailingEntries = 10;
    private const int MaxDescriptionLength = 300;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SlackHealthNotifier> _logger;
    private readonly string _webhookUrl;
    private readonly string _environment;
    private readonly string _panelUrl;
    private readonly string _logsUrl;

    public SlackHealthNotifier(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<SlackHealthNotifier> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var alerting = configuration.GetRequiredSection("Alerting");
        _webhookUrl = alerting["Slack:WebhookUrl"] ?? string.Empty;
        _environment = alerting["Environment"] ?? "Development";
        _panelUrl = alerting["PanelUrl"] ?? string.Empty;
        _logsUrl = alerting["LogsUrl"] ?? string.Empty;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_webhookUrl);

    public Task NotifyDown(string name, UIHealthReport report)
        => SendAsync(DownMessage(name, report, _environment, _panelUrl, _logsUrl));

    public Task NotifyWakeUp(string name)
        => SendAsync(WakeUpMessage(name, _environment));

    // Slack mrkdwn: *kalın*. Kontrol açıklamaları dışarıdan gelen metin içerebilir; & < > Slack'te özel anlamlı.
    internal static string DownMessage(string name, UIHealthReport report, string environment, string panelUrl, string logsUrl)
    {
        var failing = report.Entries
            .Where(entry => entry.Value.Status != UIHealthStatus.Healthy)
            .Take(MaxFailingEntries)
            .Select(entry => $"• *{Escape(entry.Key)}*: {Escape(Describe(entry.Value))}");

        var links = string.Join("  ·  ", new[]
        {
            string.IsNullOrEmpty(panelUrl) ? null : $"Panel: {panelUrl}",
            string.IsNullOrEmpty(logsUrl) ? null : $"Loglar: {logsUrl}"
        }.Where(link => link is not null));

        // Kütüphane Degraded'de de bildirim gönderir (ör. işlenemeyen olay, RabbitMQ'ya bağlanamayan worker, kapalı Kibana):
        // sistem çalışıyor ama bakılması gerekiyor. "Sağlıksız" demek yanıltıcı olur; başlık seviyeyi ayırır.
        var headline = report.Status == UIHealthStatus.Degraded
            ? $":warning: *{Escape(name)}* uyarı veriyor — {Escape(environment)}"
            : $":rotating_light: *{Escape(name)}* sağlıksız — {Escape(environment)}";

        return string.Join('\n', new[] { headline }
            .Concat(failing)
            .Concat(links.Length == 0 ? [] : [links]));
    }

    internal static string WakeUpMessage(string name, string environment)
        => $":white_check_mark: *{Escape(name)}* düzeldi — {Escape(environment)}";

    private static string Describe(UIHealthReportEntry entry)
    {
        var text = string.IsNullOrWhiteSpace(entry.Description) ? entry.Status.ToString() : entry.Description;
        return text.Length <= MaxDescriptionLength ? text : text[..MaxDescriptionLength] + "…";
    }

    private static string Escape(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    // Bildirim hatası yoklamayı bozmamalı: gönderilemezse uyarı loglanır, atılmaz.
    private async Task SendAsync(string text)
    {
        if (!IsConfigured)
            return;

        try
        {
            // Gövde uzunluğu belli gider (StringContent): PostAsJsonAsync gövdeyi parça parça (chunked) gönderir ve bazı webhook
            // alıcıları bunu kabul etmez.
            using var content = new StringContent(JsonSerializer.Serialize(new { text }), Encoding.UTF8, "application/json");
            using var response = await _httpClientFactory.CreateClient(HttpClientName).PostAsync(_webhookUrl, content);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Slack bildirimi gönderilemedi: HTTP {StatusCode}.", (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning("Slack bildirimi gönderilemedi: {Reason}.", ex.Message);
        }
    }
}
