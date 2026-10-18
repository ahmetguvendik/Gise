using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Microsoft.Extensions.Configuration;

namespace Gise.Catalog.Application.Validators;

public sealed class CreateVenueCommandValidator : AbstractValidator<CreateVenueCommand>
{
    public CreateVenueCommandValidator(IConfiguration configuration)
    {
        // Sınırlar appsettings'teki CatalogRules bölümünden okunur.
        var rules = configuration.GetRequiredSection("CatalogRules");
        var venueNameMaxLength = rules.GetValue<int>("VenueNameMaxLength");
        var cityMaxLength = rules.GetValue<int>("CityMaxLength");

        // Bir alanın ilk hatasında o alanın diğer kuralları çalışmaz; null listede Must patlamaz.
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mekân adı gereklidir.")
            .MaximumLength(venueNameMaxLength).WithMessage($"Mekân adı en fazla {venueNameMaxLength} karakter olabilir.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Şehir gereklidir.")
            .MaximumLength(cityMaxLength).WithMessage($"Şehir en fazla {cityMaxLength} karakter olabilir.");

        RuleFor(x => x.Sections).ValidVenueLayout(configuration);

        // Koleksiyonda Stop "ilk hatalı elemanda dur" demek; her bölümün hatası görünsün.
        RuleForEach(x => x.Sections).Cascade(CascadeMode.Continue).SetValidator(new VenueSectionInputValidator(configuration));
    }
}
