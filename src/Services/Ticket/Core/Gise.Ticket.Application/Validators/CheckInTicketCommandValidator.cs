using FluentValidation;
using Gise.Ticket.Application.Features.Commands.Ticket.Request;
using Gise.Ticket.Application.Tickets;

namespace Gise.Ticket.Application.Validators;

public sealed class CheckInTicketCommandValidator : AbstractValidator<CheckInTicketCommand>
{
    public CheckInTicketCommandValidator()
    {
        RuleFor(x => x.Code)
            .Must(code => TicketCode.Normalize(code) is not null)
            .WithMessage("Bilet kodu 16 karakter olmalı (ör. ABCD-EFGH-JKMN-PQRS).");

        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("Okutmadan önce kapıdaki etkinliği seçin.");
    }
}
