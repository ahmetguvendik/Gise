using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Media.Request;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.Media;

namespace Gise.Catalog.Application.Validators;

// Beyan edilen tür ve boyut; yükleme bitince depodaki gerçek dosya ayrıca kontrol edilir (CommitUploadCommand).
public sealed class CreateUploadUrlCommandValidator : AbstractValidator<CreateUploadUrlCommand>
{
    public CreateUploadUrlCommandValidator(IStorageService storageService)
    {
        // Sınır appsettings'teki Storage bölümünden okunur.
        var maxSizeBytes = storageService.MaxUploadSizeBytes;

        RuleFor(x => x.ContentType)
            .Must(MediaPolicy.IsAllowed)
            .WithMessage($"Bu dosya türü yüklenemez. İzin verilen türler: {string.Join(", ", MediaPolicy.AllowedContentTypes)}.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("Dosya boş olamaz.")
            .LessThanOrEqualTo(maxSizeBytes).WithMessage($"Dosya en fazla {maxSizeBytes / (1024 * 1024)} MB olabilir.");
    }
}
