using FluentValidation;
using Gise.Identity.Application.Features.Queries.User.Request;

namespace Gise.Identity.Application.Validators;

public sealed class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    public SearchUsersQueryValidator()
    {
        // Tek harf neredeyse herkesle eşleşir.
        RuleFor(x => x.Search)
            .Must(search => search.Trim().Length is >= 2 and <= 100)
            .WithMessage("Aramak için 2 ile 100 karakter arasında yazın.");
    }
}
