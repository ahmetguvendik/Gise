using Gise.Catalog.Application.Features.Commands.Media.Request;
using Gise.Catalog.Application.Features.Commands.Media.Response;
using Gise.Catalog.Application.Interfaces;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Media.CommandHandlers;

public sealed class CreateUploadUrlCommandHandler : IRequestHandler<CreateUploadUrlCommand, CreateUploadUrlResponse>
{
    private readonly IStorageService _storageService;

    public CreateUploadUrlCommandHandler(IStorageService storageService)
    {
        _storageService = storageService;
    }

    // Tür ve boyut CreateUploadUrlCommandValidator'da doğrulandı.
    public Task<CreateUploadUrlResponse> Handle(CreateUploadUrlCommand request, CancellationToken cancellationToken)
    {
        var upload = _storageService.CreateUploadUrl(request.ContentType);

        return Task.FromResult(new CreateUploadUrlResponse
        {
            UploadUrl = upload.UploadUrl,
            Key = upload.Key,
            ContentType = upload.ContentType,
            ExpiresInSeconds = upload.ExpiresInSeconds
        });
    }
}
