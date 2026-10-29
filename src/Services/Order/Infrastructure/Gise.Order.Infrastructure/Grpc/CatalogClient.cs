using Gise.Contracts.Grpc.Catalog;
using Gise.Exceptions;
using Gise.Order.Application.Interfaces;
using Grpc.Core;
using Microsoft.Extensions.Configuration;

namespace Gise.Order.Infrastructure.Grpc;

// Catalog'un gRPC ucu (yalnızca Docker ağının içinden, 8081). Hatalar merkezi hatalara çevrilir; müşteri sebebi görür.
public sealed class CatalogClient : ICatalogClient
{
    private readonly CatalogGrpc.CatalogGrpcClient _client;
    private readonly TimeSpan _timeout;

    public CatalogClient(CatalogGrpc.CatalogGrpcClient client, IConfiguration configuration)
    {
        _client = client;
        _timeout = TimeSpan.FromSeconds(configuration.GetRequiredSection("Catalog").GetValue<int>("GrpcTimeoutSeconds"));
    }

    public async Task<EventSummary> GetEventSummaryAsync(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            var reply = await _client.GetEventSummaryAsync(
                new GetEventSummaryRequest { EventId = eventId.ToString() },
                deadline: DateTime.UtcNow.Add(_timeout),
                cancellationToken: cancellationToken);

            return new EventSummary(reply.Title, reply.StartsAt.ToDateTime(), reply.VenueName, reply.City);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw NotFoundException.Event(eventId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
        {
            // Yayında değil, iptal edilmiş ya da başlamış.
            throw BusinessException.EventNotOnSale(eventId);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw ServiceUnavailableException.Catalog();
        }
    }
}
