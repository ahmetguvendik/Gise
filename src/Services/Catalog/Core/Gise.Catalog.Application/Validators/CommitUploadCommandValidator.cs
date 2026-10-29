using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Media.Request;

namespace Gise.Catalog.Application.Validators;

public sealed class CommitUploadCommandValidator : AbstractValidator<CommitUploadCommand>
{
    public CommitUploadCommandValidator()
    {
        // Anahtar istemciden geliyor: yalnızca yükleme adresiyle verilen geçici anahtar kabul edilir; staging dışındaki
        // bir nesne taşınamaz.
        RuleFor(x => x.Key)
            .Matches("^staging/[0-9a-f]{32}\\.(jpg|png|webp|avif)$")
            .WithMessage("Geçersiz yükleme anahtarı; yükleme adresiyle birlikte verilen geçici anahtar gönderilmelidir.");
    }
}
