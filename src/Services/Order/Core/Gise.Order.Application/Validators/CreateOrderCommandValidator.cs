using FluentValidation;
using Gise.Order.Application.Features.Commands.Order.Request;
using Microsoft.Extensions.Configuration;

namespace Gise.Order.Application.Validators;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator(IConfiguration configuration)
    {
        // Sınır appsettings'teki OrderRules bölümünden okunur.
        var maxSeats = configuration.GetRequiredSection("OrderRules").GetValue<int>("MaxSeatsPerOrder");

        RuleFor(x => x.EventId).NotEmpty().WithMessage("Etkinlik seçilmelidir.");

        RuleFor(x => x.SeatIds)
            .NotEmpty().WithMessage("En az bir koltuk seçilmelidir.")
            .Must(ids => ids.Count <= maxSeats).WithMessage($"Bir siparişte en fazla {maxSeats} koltuk olabilir.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Aynı koltuk iki kez seçilemez.");
    }
}
