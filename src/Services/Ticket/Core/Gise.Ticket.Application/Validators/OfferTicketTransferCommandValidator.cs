using FluentValidation;
using Gise.Ticket.Application.Features.Commands.TicketTransfer.Request;

namespace Gise.Ticket.Application.Validators;

public sealed class OfferTicketTransferCommandValidator : AbstractValidator<OfferTicketTransferCommand>
{
    public OfferTicketTransferCommandValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Alıcının e-posta adresini yazın.")
            .MaximumLength(254).WithMessage("E-posta adresi en fazla 254 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi yazın.");
    }
}
