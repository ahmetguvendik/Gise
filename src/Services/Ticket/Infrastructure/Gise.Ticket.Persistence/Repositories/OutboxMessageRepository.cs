using System.Diagnostics;
using System.Text.Json;
using Gise.Contracts.Events;
using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Domain.Entities;
using Gise.Ticket.Persistence.Contexts;

namespace Gise.Ticket.Persistence.Repositories;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly TicketDbContext _context;

    public OutboxMessageRepository(TicketDbContext context)
    {
        _context = context;
    }

    public async Task AddTicketEventAsync<TEvent>(Guid eventId, TEvent payload, CancellationToken cancellationToken) where TEvent : notnull
        => await _context.OutboxMessages.AddAsync(new OutboxMessage
        {
            Topic = Kafka.TicketEventsTopic,
            Type = typeof(TEvent).Name,
            Key = eventId.ToString(),
            Payload = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            TraceParent = Activity.Current?.Id
        }, cancellationToken);
}
