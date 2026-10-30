using FluentValidation;
using Gise.Search.Application.Features.Queries.Event.Request;

namespace Gise.Search.Application.Validators;

public sealed class SuggestQueryValidator : AbstractValidator<SuggestQuery>
{
    public SuggestQueryValidator()
    {
        // Tek harf neredeyse her şeyle eşleşir; öneri ikinci harften başlar.
        RuleFor(x => x.Q)
            .Must(q => q.Trim().Length is >= 2 and <= 50)
            .WithMessage("Öneri için 2 ile 50 karakter arasında yazın.");
    }
}
