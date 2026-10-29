using FluentValidation;
using Gise.Order.Application.Features.Commands.Order.Request;

namespace Gise.Order.Application.Validators;

// Mesajlarda kartın kendisi yer almaz.
public sealed class PayOrderCommandValidator : AbstractValidator<PayOrderCommand>
{
    public PayOrderCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.CardNumber)
            .Matches("^[0-9]{16}$").WithMessage("Kart numarası 16 haneli olmalı.");

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12).WithMessage("Son kullanma ayı 1 ile 12 arasında olmalı.");

        RuleFor(x => x.ExpiryYear)
            .Must((command, year) =>
            {
                var now = timeProvider.GetUtcNow();
                return (year, command.ExpiryMonth).CompareTo((now.Year, now.Month)) >= 0 && year <= now.Year + 20;
            })
            .When(x => x.ExpiryMonth is >= 1 and <= 12)
            .WithMessage("Kartın son kullanma tarihi geçmiş ya da geçersiz.");

        RuleFor(x => x.Cvc)
            .Matches("^[0-9]{3}$").WithMessage("Güvenlik kodu 3 haneli olmalı.");
    }
}
