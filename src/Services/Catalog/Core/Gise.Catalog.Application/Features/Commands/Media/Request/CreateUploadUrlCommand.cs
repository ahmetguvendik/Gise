using Gise.Catalog.Application.Features.Commands.Media.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Commands.Media.Request;

// Görsel yüklemenin ilk adımı: tarayıcının dosyayı doğrudan MinIO'ya yükleyeceği imzalı adres. Dosya API'den geçmez.
public sealed class CreateUploadUrlCommand : IRequest<CreateUploadUrlResponse>
{
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}
