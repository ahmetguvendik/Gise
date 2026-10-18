using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Microsoft.Extensions.Configuration;

namespace Gise.Catalog.Application.Validators;

// Tek bir bölüm ve sıraları; mekân oluşturma ve düzen güncelleme aynı kuralları kullanır.
public sealed class VenueSectionInputValidator : AbstractValidator<VenueSectionInput>
{
    public VenueSectionInputValidator(IConfiguration configuration)
    {
        // Sınırlar appsettings'teki CatalogRules bölümünden okunur.
        var rules = configuration.GetRequiredSection("CatalogRules");
        var sectionNameMaxLength = rules.GetValue<int>("SectionNameMaxLength");
        var rowNameMaxLength = rules.GetValue<int>("RowNameMaxLength");
        var maxSeatsPerRow = rules.GetValue<int>("MaxSeatsPerRow");

        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(s => s.Name)
            .NotEmpty().WithMessage("Bölüm adı gereklidir.")
            .MaximumLength(sectionNameMaxLength).WithMessage($"Bölüm adı en fazla {sectionNameMaxLength} karakter olabilir.");

        RuleFor(s => s.Rows)
            .NotEmpty().WithMessage("Her bölümde en az bir sıra olmalıdır.")
            .Must(rows => rows.Select(r => (r.Row ?? string.Empty).Trim().ToLowerInvariant()).Distinct().Count() == rows.Count)
            .WithMessage("Bir bölümde aynı sıra iki kez tanımlanamaz.");

        // Koleksiyonda Stop "ilk hatalı elemanda dur" demek; her sıranın hatası görünsün.
        RuleForEach(s => s.Rows).Cascade(CascadeMode.Continue).ChildRules(row =>
        {
            row.RuleFor(r => r.Row)
                .NotEmpty().WithMessage("Sıra adı gereklidir.")
                .MaximumLength(rowNameMaxLength).WithMessage($"Sıra adı en fazla {rowNameMaxLength} karakter olabilir.");

            row.RuleFor(r => r.SeatCount)
                .InclusiveBetween(1, maxSeatsPerRow)
                .WithMessage($"Bir sırada 1 ile {maxSeatsPerRow} arasında koltuk olabilir.");
        });
    }
}

public static class VenueLayoutRuleExtensions
{
    // Bölüm listesinin bütünü: en az bir bölüm, tekrarsız adlar, mekân başına koltuk sınırı.
    public static IRuleBuilderOptions<T, List<VenueSectionInput>> ValidVenueLayout<T>(
        this IRuleBuilder<T, List<VenueSectionInput>> rule, IConfiguration configuration)
    {
        var maxSeatsPerVenue = configuration.GetRequiredSection("CatalogRules").GetValue<int>("MaxSeatsPerVenue");

        return rule
            .NotEmpty().WithMessage("En az bir bölüm eklenmelidir.")
            .Must(sections => sections.Select(s => (s.Name ?? string.Empty).Trim().ToLowerInvariant()).Distinct().Count() == sections.Count)
            .WithMessage("Bölüm adları tekrar edemez.")
            .Must(sections => sections.Sum(s => s.Rows?.Sum(r => r.SeatCount) ?? 0) <= maxSeatsPerVenue)
            .WithMessage($"Bir mekânda en fazla {maxSeatsPerVenue} koltuk olabilir.");
    }
}
