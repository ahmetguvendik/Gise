using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Microsoft.Extensions.Configuration;

namespace Gise.Catalog.Application.Validators;

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator(TimeProvider timeProvider, IConfiguration configuration)
    {
        // Sınır appsettings'teki CatalogRules bölümünden okunur.
        var titleMaxLength = configuration.GetRequiredSection("CatalogRules").GetValue<int>("EventTitleMaxLength");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Etkinlik adı gereklidir.")
            .MaximumLength(titleMaxLength)
            .WithMessage($"Etkinlik adı en fazla {titleMaxLength} karakter olabilir.");

        RuleFor(x => x.VenueId)
            .NotEmpty().WithMessage("Mekân seçilmelidir.");

        RuleFor(x => x.StartsAt)
            .Must(startsAt => startsAt > timeProvider.GetUtcNow())
            .WithMessage("Etkinlik tarihi gelecekte olmalıdır.");
    }
}
