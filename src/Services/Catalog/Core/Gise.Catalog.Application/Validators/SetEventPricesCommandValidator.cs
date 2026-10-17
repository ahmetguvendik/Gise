using System.Globalization;
using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Microsoft.Extensions.Configuration;

namespace Gise.Catalog.Application.Validators;

public sealed class SetEventPricesCommandValidator : AbstractValidator<SetEventPricesCommand>
{
    public SetEventPricesCommandValidator(IConfiguration configuration)
    {
        // Sınır appsettings'teki CatalogRules bölümünden okunur.
        var maxPrice = configuration.GetRequiredSection("CatalogRules").GetValue<decimal>("MaxPrice");

        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Prices)
            .NotEmpty().WithMessage("En az bir bölümün fiyatı girilmelidir.")
            .Must(prices => prices.Select(p => p.SectionId).Distinct().Count() == prices.Count)
            .WithMessage("Bir bölüm listede birden fazla kez olamaz.");

        RuleForEach(x => x.Prices).Cascade(CascadeMode.Continue).ChildRules(item =>
        {
            item.RuleLevelCascadeMode = CascadeMode.Stop;

            item.RuleFor(p => p.SectionId)
                .NotEmpty().WithMessage("Bölüm seçilmelidir.");

            item.RuleFor(p => p.Price)
                .GreaterThan(0).WithMessage("Fiyat 0'dan büyük olmalıdır.")
                .LessThanOrEqualTo(maxPrice).WithMessage($"Fiyat en fazla {maxPrice.ToString("#,0.##", CultureInfo.GetCultureInfo("tr-TR"))} ₺ olabilir.")
                .PrecisionScale(10, 2, ignoreTrailingZeros: true).WithMessage("Fiyat en fazla 2 ondalık basamak içerebilir.");
        });
    }
}
