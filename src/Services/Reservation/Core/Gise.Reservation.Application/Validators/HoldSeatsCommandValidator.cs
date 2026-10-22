using FluentValidation;
using Gise.Reservation.Application.Features.Commands.SeatHold.Request;
using Microsoft.Extensions.Configuration;

namespace Gise.Reservation.Application.Validators;

public sealed class HoldSeatsCommandValidator : AbstractValidator<HoldSeatsCommand>
{
    public HoldSeatsCommandValidator(IConfiguration configuration)
    {
        // Sınır appsettings'teki ReservationRules bölümünden okunur.
        var maxSeats = configuration.GetRequiredSection("ReservationRules").GetValue<int>("MaxSeatsPerHold");

        RuleFor(x => x.EventId).NotEmpty().WithMessage("Etkinlik seçilmelidir.");

        RuleFor(x => x.SeatIds)
            .NotEmpty().WithMessage("En az bir koltuk seçilmelidir.")
            .Must(ids => ids.Count <= maxSeats).WithMessage($"Bir seferde en fazla {maxSeats} koltuk tutulabilir.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Aynı koltuk iki kez seçilemez.");
    }
}
