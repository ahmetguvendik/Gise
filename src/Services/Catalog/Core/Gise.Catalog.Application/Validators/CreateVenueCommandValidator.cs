using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Gise.Catalog.Domain.Entities;

namespace Gise.Catalog.Application.Validators;

public sealed class CreateVenueCommandValidator : AbstractValidator<CreateVenueCommand>
{
    public const int MaxSeats = 5000;

    public CreateVenueCommandValidator()
    {
        // Bir alanın ilk hatasında o alanın diğer kuralları çalışmaz; null listede Must patlamaz.
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mekân adı gereklidir.")
            .MaximumLength(Venue.NameMaxLength).WithMessage($"Mekân adı en fazla {Venue.NameMaxLength} karakter olabilir.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Şehir gereklidir.")
            .MaximumLength(Venue.CityMaxLength).WithMessage($"Şehir en fazla {Venue.CityMaxLength} karakter olabilir.");

        RuleFor(x => x.Sections)
            .NotEmpty().WithMessage("En az bir bölüm eklenmelidir.")
            .Must(sections => sections.Select(s => (s.Name ?? string.Empty).Trim().ToLowerInvariant()).Distinct().Count() == sections.Count)
            .WithMessage("Bölüm adları tekrar edemez.")
            .Must(sections => sections.Sum(s => s.Rows?.Sum(r => r.SeatCount) ?? 0) <= MaxSeats)
            .WithMessage($"Bir mekânda en fazla {MaxSeats} koltuk olabilir.");

        // Koleksiyonda Stop "ilk hatalı elemanda dur" demek; her elemanın hatası görünsün.
        RuleForEach(x => x.Sections).Cascade(CascadeMode.Continue).ChildRules(section =>
        {
            section.RuleLevelCascadeMode = CascadeMode.Stop;

            section.RuleFor(s => s.Name)
                .NotEmpty().WithMessage("Bölüm adı gereklidir.")
                .MaximumLength(Section.NameMaxLength).WithMessage($"Bölüm adı en fazla {Section.NameMaxLength} karakter olabilir.");

            section.RuleFor(s => s.Rows)
                .NotEmpty().WithMessage("Her bölümde en az bir sıra olmalıdır.")
                .Must(rows => rows.Select(r => (r.Row ?? string.Empty).Trim().ToLowerInvariant()).Distinct().Count() == rows.Count)
                .WithMessage("Bir bölümde aynı sıra iki kez tanımlanamaz.");

            section.RuleForEach(s => s.Rows).Cascade(CascadeMode.Continue).ChildRules(row =>
            {
                row.RuleFor(r => r.Row)
                    .NotEmpty().WithMessage("Sıra adı gereklidir.")
                    .MaximumLength(Seat.RowMaxLength).WithMessage($"Sıra adı en fazla {Seat.RowMaxLength} karakter olabilir.");

                row.RuleFor(r => r.SeatCount)
                    .InclusiveBetween(1, Section.MaxSeatsPerRow)
                    .WithMessage($"Bir sırada 1 ile {Section.MaxSeatsPerRow} arasında koltuk olabilir.");
            });
        });
    }
}
