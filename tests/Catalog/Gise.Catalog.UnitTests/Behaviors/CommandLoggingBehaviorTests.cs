using Gise.Catalog.Application.Auditing;
using Gise.Catalog.Application.Behaviors;
using Gise.Exceptions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Gise.Catalog.UnitTests.Behaviors;

// Denetim satırı (kim ne yaptı): alanlar, başarısız deneme ve kişisel verinin loga girmemesi.
public sealed class CommandLoggingBehaviorTests
{
    private sealed record UpdateVenueCommand(Guid Id, string Name, string City);

    private sealed record UpdateVenueResponse(Guid Id);

    // Bilet aktarımındaki gibi: Name yeni sahibin adı, Email adresi.
    private sealed record AcceptTicketTransferCommand(Guid TransferId, string Name, string Email);

    private sealed record GetVenueQuery(Guid Id);

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private readonly CollectingSink _sink = new();
    private readonly ChangeAuditCollector _changes = new();

    private CommandLoggingBehavior<TRequest, TResponse> Behavior<TRequest, TResponse>() where TRequest : notnull
    {
        var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(_sink).CreateLogger();
        var factory = new SerilogLoggerFactory(logger, dispose: true);
        return new CommandLoggingBehavior<TRequest, TResponse>(
            factory.CreateLogger<CommandLoggingBehavior<TRequest, TResponse>>(), _changes);
    }

    private static string? Text(LogEvent logEvent, string property) =>
        logEvent.Properties.TryGetValue(property, out var value) && value is ScalarValue { Value: var scalar }
            ? scalar?.ToString()
            : null;

    [Fact]
    public async Task Basarili_command_kim_ne_hangi_kayit_ve_degisenlerle_yazilir()
    {
        var venueId = Guid.NewGuid();

        await Behavior<UpdateVenueCommand, UpdateVenueResponse>().Handle(
            new UpdateVenueCommand(venueId, "Meram Kültür Merkezi", "Konya"),
            _ =>
            {
                _changes.Add("Venue.Name: 'Meram' → 'Meram Kültür Merkezi'");
                _changes.Add("Seat.Row: 'A' → 'B'");
                _changes.Add("Seat.Row: 'A' → 'B'");
                return Task.FromResult(new UpdateVenueResponse(venueId));
            },
            CancellationToken.None);

        var logEvent = Assert.Single(_sink.Events);
        Assert.Equal(LogEventLevel.Information, logEvent.Level);
        Assert.Equal("gise.audit", Text(logEvent, "event.dataset"));
        Assert.Equal("UpdateVenue", Text(logEvent, "event.action"));
        Assert.Equal("success", Text(logEvent, "event.outcome"));
        Assert.Equal(venueId.ToString(), Text(logEvent, "target.venue.id"));
        Assert.Equal("Meram Kültür Merkezi", Text(logEvent, "target.venue.name"));
        Assert.Contains("Seat.Row: 'A' → 'B' (×2)", logEvent.RenderMessage());
        Assert.Empty(_changes.Drain());
    }

    [Fact]
    public async Task Basarisiz_deneme_hata_koduyla_yazilir_ve_hata_aynen_firlar()
    {
        var eventId = Guid.NewGuid();
        var behavior = Behavior<UpdateVenueCommand, UpdateVenueResponse>();

        var thrown = await Assert.ThrowsAsync<ForbiddenException>(() => behavior.Handle(
            new UpdateVenueCommand(Guid.NewGuid(), "Meram", "Konya"),
            _ =>
            {
                _changes.Add("Venue.Name: 'A' → 'B'");
                throw ForbiddenException.NotEventOwner(eventId);
            },
            CancellationToken.None));

        var logEvent = Assert.Single(_sink.Events);
        Assert.Equal(LogEventLevel.Warning, logEvent.Level);
        Assert.Equal("failure", Text(logEvent, "event.outcome"));
        Assert.Equal(thrown.ErrorCode, Text(logEvent, "error.code"));
        Assert.Equal("403", Text(logEvent, "StatusCode"));
        // Kaydedilemeyen değişiklik bir sonraki command'a karışmaz.
        Assert.Empty(_changes.Drain());
    }

    [Fact]
    public async Task Kisi_adi_ve_e_posta_loga_girmez()
    {
        var transferId = Guid.NewGuid();

        await Behavior<AcceptTicketTransferCommand, Guid>().Handle(
            new AcceptTicketTransferCommand(transferId, "Ayşe Yılmaz", "ayse@example.com"),
            _ => Task.FromResult(Guid.NewGuid()),
            CancellationToken.None);

        var logEvent = Assert.Single(_sink.Events);
        Assert.Equal(transferId.ToString(), Text(logEvent, "target.transfer.id"));
        Assert.DoesNotContain(logEvent.Properties.Values, value =>
            value.ToString().Contains("Ayşe") || value.ToString().Contains("ayse@example.com"));
    }

    [Fact]
    public async Task Query_yazilmaz()
    {
        await Behavior<GetVenueQuery, string>().Handle(
            new GetVenueQuery(Guid.NewGuid()), _ => Task.FromResult("Meram"), CancellationToken.None);

        Assert.Empty(_sink.Events);
    }
}
