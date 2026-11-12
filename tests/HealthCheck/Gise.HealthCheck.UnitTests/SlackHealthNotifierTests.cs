using System.Net;
using Gise.HealthCheck.Api.Notifications;
using HealthChecks.UI.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gise.HealthCheck.UnitTests;

public sealed class SlackHealthNotifierTests
{
    private static UIHealthReportEntry Entry(UIHealthStatus status, string? description) => new() { Status = status, Description = description };

    // Gerçek raporda genel durumu kütüphane girişlerin en kötüsü olarak belirler; aynısı burada.
    private static UIHealthReport Report(params (string Name, UIHealthStatus Status, string? Description)[] entries)
        => new(entries.ToDictionary(e => e.Name, e => Entry(e.Status, e.Description)), TimeSpan.Zero)
        {
            Status = entries.Any(e => e.Status == UIHealthStatus.Unhealthy) ? UIHealthStatus.Unhealthy
                : entries.Any(e => e.Status == UIHealthStatus.Degraded) ? UIHealthStatus.Degraded
                : UIHealthStatus.Healthy
        };

    [Fact]
    public void Degraded_is_a_warning_not_an_outage()
    {
        var degraded = SlackHealthNotifier.DownMessage("order-saga-worker", Report(("masstransit-bus", UIHealthStatus.Degraded, "Degraded Endpoints: quartz")), "Test", "", "");
        var unhealthy = SlackHealthNotifier.DownMessage("altyapi", Report(("rabbitmq", UIHealthStatus.Unhealthy, "kapalı")), "Test", "", "");

        Assert.StartsWith(":warning: *order-saga-worker* uyarı veriyor — Test", degraded);
        Assert.DoesNotContain("sağlıksız", degraded);
        Assert.StartsWith(":rotating_light: *altyapi* sağlıksız — Test", unhealthy);
    }

    private static SlackHealthNotifier Notifier(FakeHandler handler, string? webhookUrl = "https://hooks.slack.test/T/B/x")
        => new(new FakeHttpClientFactory(handler), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Alerting:Slack:WebhookUrl"] = webhookUrl,
            ["Alerting:Environment"] = "Test",
            ["Alerting:PanelUrl"] = "http://panel.test",
            ["Alerting:LogsUrl"] = "http://logs.test"
        }).Build(), NullLogger<SlackHealthNotifier>.Instance);

    [Fact]
    public void Down_message_names_the_service_environment_failing_checks_and_reasons_only()
    {
        var report = Report(
            ("postgres", UIHealthStatus.Healthy, "tamam"),
            ("rabbitmq", UIHealthStatus.Unhealthy, "rabbitmq:5672 bağlantı kurulamadı: ConnectionRefused."));

        var text = SlackHealthNotifier.DownMessage("altyapi", report, "Test", "http://panel.test", "http://logs.test");

        Assert.Equal(
            ":rotating_light: *altyapi* sağlıksız — Test\n• *rabbitmq*: rabbitmq:5672 bağlantı kurulamadı: ConnectionRefused.\nPanel: http://panel.test  ·  Loglar: http://logs.test",
            text);
        Assert.DoesNotContain("postgres", text);
    }

    [Fact]
    public void Entry_without_description_falls_back_to_its_status_and_slack_markup_is_escaped()
    {
        var report = Report(("a", UIHealthStatus.Unhealthy, null), ("b<script>", UIHealthStatus.Degraded, "x & y > z"));

        var text = SlackHealthNotifier.DownMessage("svc", report, "Test", "", "");

        Assert.Contains("• *a*: Unhealthy", text);
        Assert.Contains("• *b&lt;script&gt;*: x &amp; y &gt; z", text);
        Assert.DoesNotContain("Panel:", text);
    }

    [Fact]
    public void Long_reasons_and_many_failing_checks_are_cut()
    {
        var entries = Enumerable.Range(0, 15).Select(i => ($"c{i}", UIHealthStatus.Unhealthy, (string?)new string('x', 500))).ToArray();

        var text = SlackHealthNotifier.DownMessage("svc", Report(entries), "Test", "", "");

        Assert.Equal(10, text.Split('\n').Count(line => line.StartsWith("• ")));
        Assert.DoesNotContain(new string('x', 301), text);
    }

    [Fact]
    public void Wake_up_message_is_short()
        => Assert.Equal(":white_check_mark: *catalog-api* düzeldi — Test", SlackHealthNotifier.WakeUpMessage("catalog-api", "Test"));

    [Fact]
    public async Task Posts_the_text_as_json_to_the_webhook()
    {
        var handler = new FakeHandler();

        await Notifier(handler).NotifyDown("catalog-api", Report(("postgres", UIHealthStatus.Unhealthy, "bağlanılamadı")));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://hooks.slack.test/T/B/x", request.Uri!.ToString());
        // Türkçe harfler JSON'da \uXXXX olarak kaçışlıdır (geçerli JSON); çözülmüş metne bakılır.
        using var body = System.Text.Json.JsonDocument.Parse(request.Body!);
        var text = body.RootElement.GetProperty("text").GetString();
        Assert.Contains("catalog-api", text);
        Assert.Contains("bağlanılamadı", text);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Without_a_webhook_nothing_is_sent(string? url)
    {
        var handler = new FakeHandler();
        var notifier = Notifier(handler, url);

        await notifier.NotifyDown("x", Report(("a", UIHealthStatus.Unhealthy, "b")));
        await notifier.NotifyWakeUp("x");

        Assert.False(notifier.IsConfigured);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task A_failing_webhook_never_breaks_the_polling()
    {
        await Notifier(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden))).NotifyWakeUp("x");
        await Notifier(new FakeHandler(throws: new HttpRequestException("ağ yok"))).NotifyDown("x", Report(("a", UIHealthStatus.Unhealthy, "b")));
    }
}
