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
        var sectionNameMaxLength = rules.GetValue<int>("SectionNameMaxLength");
        var rowNameMaxLength = rules.GetValue<int>("RowNameMaxLength");
        var maxSeatsPerRow = rules.GetValue<int>("MaxSeatsPerRow");
        var maxSeatsPerVenue = rules.GetValue<int>("MaxSeatsPerVenue");

        // Bir alanın ilk hatasında o alanın diğer kuralları çalışmaz; null listede Must patlamaz.
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mekân adı gereklidir.")
            .MaximumLength(venueNameMaxLength).WithMessage($"Mekân adı en fazla {venueNameMaxLength} karakter olabilir.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Şehir gereklidir.")
            .MaximumLength(cityMaxLength).WithMessage($"Şehir en fazla {cityMaxLength} karakter olabilir.");

        RuleFor(x => x.Sections)
            .NotEmpty().WithMessage("En az bir bölüm eklenmelidir.")
            .Must(sections => sections.Select(s => (s.Name ?? string.Empty).Trim().ToLowerInvariant()).Distinct().Count() == sections.Count)
            .WithMessage("Bölüm adları tekrar edemez.")
            .Must(sections => sections.Sum(s => s.Rows?.Sum(r => r.SeatCount) ?? 0) <= maxSeatsPerVenue)
            .WithMessage($"Bir mekânda en fazla {maxSeatsPerVenue} koltuk olabilir.");

        // Koleksiyonda Stop "ilk hatalı elemanda dur" demek; her elemanın hatası görünsün.
        RuleForEach(x => x.Sections).Cascade(CascadeMode.Continue).ChildRules(section =>
        {
            section.RuleLevelCascadeMode = CascadeMode.Stop;

            section.RuleFor(s => s.Name)
                .NotEmpty().WithMessage("Bölüm adı gereklidir.")
                .MaximumLength(sectionNameMaxLength).WithMessage($"Bölüm adı en fazla {sectionNameMaxLength} karakter olabilir.");

            section.RuleFor(s => s.Rows)
                .NotEmpty().WithMessage("Her bölümde en az bir sıra olmalıdır.")
                .Must(rows => rows.Select(r => (r.Row ?? string.Empty).Trim().ToLowerInvariant()).Distinct().Count() == rows.Count)
                .WithMessage("Bir bölümde aynı sıra iki kez tanımlanamaz.");

            section.RuleForEach(s => s.Rows).Cascade(CascadeMode.Continue).ChildRules(row =>
            {
                row.RuleFor(r => r.Row)
                    .NotEmpty().WithMessage("Sıra adı gereklidir.")
                    .MaximumLength(rowNameMaxLength).WithMessage($"Sıra adı en fazla {rowNameMaxLength} karakter olabilir.");

                row.RuleFor(r => r.SeatCount)
                    .InclusiveBetween(1, maxSeatsPerRow)
                    .WithMessage($"Bir sırada 1 ile {maxSeatsPerRow} arasında koltuk olabilir.");
            });
        });
    }
}
