using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Microsoft.Extensions.Configuration;

namespace Gise.Catalog.Application.Validators;

public sealed class UpdateVenueCommandValidator : AbstractValidator<UpdateVenueCommand>
{
    public UpdateVenueCommandValidator(IConfiguration configuration)
    {
        // Sınırlar appsettings'teki CatalogRules bölümünden okunur.
        var rules = configuration.GetRequiredSection("CatalogRules");
        var venueNameMaxLength = rules.GetValue<int>("VenueNameMaxLength");
        var cityMaxLength = rules.GetValue<int>("CityMaxLength");

        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mekân adı gereklidir.")
            .MaximumLength(venueNameMaxLength).WithMessage($"Mekân adı en fazla {venueNameMaxLength} karakter olabilir.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Şehir gereklidir.")
            .MaximumLength(cityMaxLength).WithMessage($"Şehir en fazla {cityMaxLength} karakter olabilir.");
    }
}
