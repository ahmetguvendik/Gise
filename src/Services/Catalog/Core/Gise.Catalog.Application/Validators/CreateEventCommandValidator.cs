using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Event.Request;

namespace Gise.Catalog.Application.Validators;

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Etkinlik adı gereklidir.")
            .MaximumLength(Domain.Entities.Event.TitleMaxLength)
            .WithMessage($"Etkinlik adı en fazla {Domain.Entities.Event.TitleMaxLength} karakter olabilir.");

        RuleFor(x => x.Venue)
            .NotEmpty().WithMessage("Mekân gereklidir.")
            .MaximumLength(Domain.Entities.Event.VenueMaxLength)
            .WithMessage($"Mekân en fazla {Domain.Entities.Event.VenueMaxLength} karakter olabilir.");

        RuleFor(x => x.StartsAt)
            .Must(startsAt => startsAt > timeProvider.GetUtcNow())
            .WithMessage("Etkinlik tarihi gelecekte olmalıdır.");
    }
}
