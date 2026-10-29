using Gise.Catalog.Application.Features.Commands.Media.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Commands.Media.Request;

// Görsel yüklemenin son adımı: geçici dosya doğrulanır, boyları üretilir, kalıcı anahtar döner.
public sealed class CommitUploadCommand : IRequest<CommitUploadResponse>
{
    // CreateUploadUrlCommand'ın verdiği geçici anahtar.
    public string Key { get; set; } = string.Empty;
}
