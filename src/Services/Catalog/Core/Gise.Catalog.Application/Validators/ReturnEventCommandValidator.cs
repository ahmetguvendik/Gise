using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Microsoft.Extensions.Configuration;

namespace Gise.Catalog.Application.Validators;

public sealed class ReturnEventCommandValidator : AbstractValidator<ReturnEventCommand>
{
    public ReturnEventCommandValidator(IConfiguration configuration)
    {
        // Sınır appsettings'teki CatalogRules bölümünden okunur.
        var reasonMaxLength = configuration.GetRequiredSection("CatalogRules").GetValue<int>("ReturnReasonMaxLength");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Geri gönderme sebebi yazılmalıdır; organizatör neyi düzelteceğini bilmeli.")
            .MaximumLength(reasonMaxLength).WithMessage($"Sebep en fazla {reasonMaxLength} karakter olabilir.");
    }
}
