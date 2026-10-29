using Gise.Catalog.Application.Features.Commands.Media.Request;
using Gise.Catalog.Application.Features.Commands.Media.Response;
using Gise.Catalog.Application.Interfaces;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Media.CommandHandlers;

public sealed class CommitUploadCommandHandler : IRequestHandler<CommitUploadCommand, CommitUploadResponse>
{
    private readonly IStorageService _storageService;

    public CommitUploadCommandHandler(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<CommitUploadResponse> Handle(CommitUploadCommand request, CancellationToken cancellationToken)
    {
        // Anahtarın biçimi CommitUploadCommandValidator'da doğrulandı; dosyanın kendisi (gerçek tür, boyut, içerik)
        // depoda kontrol edilir.
        var key = await _storageService.CommitAsync(request.Key, cancellationToken);

        return new CommitUploadResponse
        {
            Key = key,
            Url = _storageService.ResolvePublicUrl(key)
        };
    }
}
