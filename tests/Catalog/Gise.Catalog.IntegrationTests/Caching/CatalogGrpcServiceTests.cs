using Gise.Catalog.Api.GrpcServices;
using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Contracts.Grpc.Catalog;
using Grpc.Core;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Catalog.IntegrationTests.Caching;

// "Başladı mı" kontrolü önbelleğe girmez: özet önbellekten gelmiş olabilir, servis saate her çağrıda bakar. Sorgu burada
// sahte bir handler'dır: başlamış bir etkinliğin özeti "önbellekten" gelmiş gibi verilir.
public sealed class CatalogGrpcServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class SummaryStub
    {
        public DateTime StartsAt { get; init; }
    }

    private sealed class FixedSummaryHandler(SummaryStub stub) : IRequestHandler<GetEventSummaryQuery, GetEventSummaryResponse>
    {
        public Task<GetEventSummaryResponse> Handle(GetEventSummaryQuery request, CancellationToken cancellationToken)
            => Task.FromResult(new GetEventSummaryResponse
            {
                EventId = request.EventId, Title = "Duman Konseri", StartsAt = stub.StartsAt, VenueName = "Test Sahnesi", City = "Ankara"
            });
    }

    // Servis yalnızca CancellationToken'ı kullanır; geri kalanı gRPC'nin soyut üyeleri.
    private sealed class FakeContext : ServerCallContext
    {
        protected override string MethodCore => "GetEventSummary";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "test";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => new();
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => new();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new(null, new Dictionary<string, List<AuthProperty>>());
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }

    private static CatalogGrpcService Service(DateTime startsAt)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CatalogGrpcServiceTests>());
        services.AddSingleton(new SummaryStub { StartsAt = startsAt });
        return new CatalogGrpcService(services.BuildServiceProvider().GetRequiredService<IMediator>(), new FixedTime(Now));
    }

    [Fact]
    public async Task Baslamis_etkinligin_ozeti_FailedPrecondition_doner()
    {
        var service = Service(Now.UtcDateTime.AddHours(-1));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetEventSummary(new GetEventSummaryRequest { EventId = Guid.NewGuid().ToString() }, new FakeContext()));

        Assert.Equal(StatusCode.FailedPrecondition, ex.StatusCode);
    }

    [Fact]
    public async Task Baslamamis_etkinligin_ozeti_doner()
    {
        var service = Service(Now.UtcDateTime.AddHours(1));
        var eventId = Guid.NewGuid();

        var reply = await service.GetEventSummary(new GetEventSummaryRequest { EventId = eventId.ToString() }, new FakeContext());

        Assert.Equal(eventId.ToString(), reply.EventId);
        Assert.Equal("Duman Konseri", reply.Title);
        Assert.Equal("Test Sahnesi", reply.VenueName);
    }
}
