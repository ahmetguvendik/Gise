using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Event.Request;

namespace Gise.Catalog.Application.Validators;

public sealed class SetEventPosterCommandValidator : AbstractValidator<SetEventPosterCommand>
{
    public SetEventPosterCommandValidator()
    {
        // Yalnızca yükleme sonucunda dönen anahtar kabul edilir; dış adres ya da başka bir nesne yazılamaz.
        RuleFor(x => x.PosterKey)
            .Matches("^posters/[0-9]{4}/[0-9]{2}/[0-9a-f]{32}$")
            .When(x => !string.IsNullOrEmpty(x.PosterKey))
            .WithMessage("Afiş, yükleme sonucunda dönen anahtarla kaydedilmelidir.");
    }
}
