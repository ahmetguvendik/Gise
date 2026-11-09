using Gise.Exceptions;
using Gise.Identity.Application.Behaviors;
using Gise.Identity.Application.Features.Commands.User.Request;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Gise.Identity.UnitTests.Users;

// Rol verme/alma denetim satırı: kim (istekten gelir), ne, kime, hangi rol, sonuç.
public sealed class RoleAuditTests
{
    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private readonly CollectingSink _sink = new();

    private CommandLoggingBehavior<TRequest, Unit> Behavior<TRequest>() where TRequest : notnull
    {
        var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(_sink).CreateLogger();
        return new CommandLoggingBehavior<TRequest, Unit>(new SerilogLoggerFactory(logger, dispose: true).CreateLogger<CommandLoggingBehavior<TRequest, Unit>>());
    }

    private static string? Field(LogEvent e, string name) => e.Properties.TryGetValue(name, out var v) ? (v as ScalarValue)?.Value?.ToString() : null;

    [Fact]
    public async Task Grant_is_written_with_target_user_and_role()
    {
        var command = new GrantRoleCommand { UserId = "22222222-2222-2222-2222-222222222222", Role = "organizer", RequestedBy = "11111111-1111-1111-1111-111111111111" };

        await Behavior<GrantRoleCommand>().Handle(command, _ => Task.FromResult(Unit.Value), TestContext.Current.CancellationToken);

        var line = Assert.Single(_sink.Events);
        Assert.Equal("gise.audit", Field(line, "event.dataset"));
        Assert.Equal("GrantRole", Field(line, "event.action"));
        Assert.Equal("success", Field(line, "event.outcome"));
        Assert.Equal("22222222-2222-2222-2222-222222222222", Field(line, "target.user.id"));
        Assert.Equal("organizer", Field(line, "target.role.name"));
        Assert.Null(Field(line, "target.requestedby.id"));
    }

    [Fact]
    public async Task Refused_revoke_is_written_as_failure()
    {
        var command = new RevokeRoleCommand { UserId = "11111111-1111-1111-1111-111111111111", Role = "admin", RequestedBy = "11111111-1111-1111-1111-111111111111" };

        await Assert.ThrowsAsync<BusinessException>(() => Behavior<RevokeRoleCommand>()
            .Handle(command, _ => throw BusinessException.CannotRevokeOwnAdmin(), TestContext.Current.CancellationToken));

        var line = Assert.Single(_sink.Events);
        Assert.Equal(LogEventLevel.Warning, line.Level);
        Assert.Equal("failure", Field(line, "event.outcome"));
        Assert.Equal(ErrorCodes.User.CannotRevokeOwnAdmin, Field(line, "error.code"));
    }
}
