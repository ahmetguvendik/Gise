using FluentValidation;
using Gise.Identity.Application.Features.Queries.User.Request;

namespace Gise.Identity.Application.Validators;

public sealed class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    public SearchUsersQueryValidator()
    {
        // Boşsa bütün kullanıcılar; tek harf neredeyse herkesle eşleşir.
        RuleFor(x => x.Search)
            .Must(search => (search ?? string.Empty).Trim().Length is 0 or (>= 2 and <= 100))
            .WithMessage("Aramak için 2 ile 100 karakter arasında yazın.");

        RuleFor(x => x.First)
            .InclusiveBetween(0, 10_000)
            .WithMessage("Geçersiz sayfa.");
    }
}
