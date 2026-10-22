using System.Diagnostics;
using System.Text.Json;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Domain.Entities;
using Gise.Catalog.Persistence.Contexts;
using Gise.Contracts.Events;

namespace Gise.Catalog.Persistence.Repositories;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly CatalogDbContext _context;

    public OutboxMessageRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task AddCatalogEventAsync<TEvent>(Guid eventId, TEvent payload, CancellationToken cancellationToken) where TEvent : notnull
        => await _context.OutboxMessages.AddAsync(new OutboxMessage
        {
            Topic = Kafka.CatalogEventsTopic,
            Type = typeof(TEvent).Name,
            Key = eventId.ToString(),
            Payload = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            TraceParent = Activity.Current?.Id
        }, cancellationToken);
}
