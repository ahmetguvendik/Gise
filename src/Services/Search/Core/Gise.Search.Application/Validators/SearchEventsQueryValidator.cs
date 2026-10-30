using FluentValidation;
using Gise.Search.Application.Features.Queries.Event.Request;

namespace Gise.Search.Application.Validators;

public sealed class SearchEventsQueryValidator : AbstractValidator<SearchEventsQuery>
{
    public SearchEventsQueryValidator()
    {
        RuleFor(x => x.Q).MaximumLength(100).WithMessage("Arama en fazla 100 karakter olabilir.");
        RuleFor(x => x.City).MaximumLength(100).WithMessage("Şehir en fazla 100 karakter olabilir.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa 1'den başlar.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 48).WithMessage("Sayfada 1 ile 48 arasında etkinlik olabilir.");
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("Bitiş tarihi başlangıçtan önce olamaz.");
    }
}
