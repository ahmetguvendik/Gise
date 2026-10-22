using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Microsoft.Extensions.Configuration;

namespace Gise.Catalog.Application.Validators;

public sealed class CancelEventCommandValidator : AbstractValidator<CancelEventCommand>
{
    public CancelEventCommandValidator(IConfiguration configuration)
    {
        // Sınır appsettings'teki CatalogRules bölümünden okunur.
        var reasonMaxLength = configuration.GetRequiredSection("CatalogRules").GetValue<int>("CancellationReasonMaxLength");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("İptal sebebi yazılmalıdır; bilet sahiplerine iletilecek.")
            .MaximumLength(reasonMaxLength).WithMessage($"İptal sebebi en fazla {reasonMaxLength} karakter olabilir.");
    }
}
