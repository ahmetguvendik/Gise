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

        RuleFor(x => x.Page)
            .InclusiveBetween(1, 10_000)
            .WithMessage("Sayfa 1 ile 10.000 arasında olmalı.");

        // Her kullanıcının rolleri ayrı istekle okunduğu için sayfa küçük tutulur.
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("Sayfa boyutu 1 ile 50 arasında olmalı.");
    }
}
