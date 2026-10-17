using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Domain.Constants;

namespace Gise.Catalog.Application.Validators;

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Etkinlik adı gereklidir.")
            .MaximumLength(EventConstants.TitleMaxLength)
            .WithMessage($"Etkinlik adı en fazla {EventConstants.TitleMaxLength} karakter olabilir.");

        RuleFor(x => x.VenueId)
            .NotEmpty().WithMessage("Mekân seçilmelidir.");

        RuleFor(x => x.StartsAt)
            .Must(startsAt => startsAt > timeProvider.GetUtcNow())
            .WithMessage("Etkinlik tarihi gelecekte olmalıdır.");
    }
}
