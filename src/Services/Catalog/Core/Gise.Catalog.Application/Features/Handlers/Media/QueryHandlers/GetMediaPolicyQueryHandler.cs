using Gise.Catalog.Application.Features.Queries.Media.Request;
using Gise.Catalog.Application.Features.Queries.Media.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.Media;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Media.QueryHandlers;

public sealed class GetMediaPolicyQueryHandler : IRequestHandler<GetMediaPolicyQuery, GetMediaPolicyResponse>
{
    private readonly IStorageService _storageService;

    public GetMediaPolicyQueryHandler(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public Task<GetMediaPolicyResponse> Handle(GetMediaPolicyQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new GetMediaPolicyResponse
        {
            MaxSizeBytes = _storageService.MaxUploadSizeBytes,
            AllowedContentTypes = [.. MediaPolicy.AllowedContentTypes]
        });
}
