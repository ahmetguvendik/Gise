using FluentValidation;
using Gise.Audit.Application.Features.Queries.Record.Request;

namespace Gise.Audit.Application.Validators;

public sealed class SearchAuditRecordsQueryValidator : AbstractValidator<SearchAuditRecordsQuery>
{
    // Elasticsearch ilk 10.000 kaydın ötesine sayfalamaz; süzgeçle daraltmak gerekir.
    private const int MaxWindow = 10_000;

    public SearchAuditRecordsQueryValidator()
    {
        RuleFor(x => x.Action)
            .Must(action => string.IsNullOrEmpty(action) || (action.Length <= 100 && action.All(char.IsLetterOrDigit)))
            .WithMessage("Olay türü yalnızca harf ve rakamdan oluşmalı (en fazla 100 karakter).");

        RuleFor(x => x.Outcome)
            .Must(outcome => string.IsNullOrEmpty(outcome) || outcome is "success" or "failure" or "unknown")
            .WithMessage("Sonuç success, failure ya da unknown olmalı.");

        RuleFor(x => x.ActorId)
            .Must(actor => string.IsNullOrEmpty(actor) || actor.Length <= 100)
            .WithMessage("Kullanıcı kimliği en fazla 100 karakter olabilir.");

        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithName(nameof(SearchAuditRecordsQuery.To))
            .WithMessage("Bitiş tarihi başlangıçtan önce olamaz.");

        RuleFor(x => x.Page)
            .InclusiveBetween(1, MaxWindow)
            .WithMessage($"Sayfa 1 ile {MaxWindow:N0} arasında olmalı.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Sayfa boyutu 1 ile 100 arasında olmalı.");

        RuleFor(x => x)
            .Must(x => (long)x.Page * x.PageSize <= MaxWindow)
            .WithName(nameof(SearchAuditRecordsQuery.Page))
            .WithMessage($"En fazla ilk {MaxWindow:N0} kayda ulaşılabilir; tarih ya da olay türüyle daraltın.");
    }
}
