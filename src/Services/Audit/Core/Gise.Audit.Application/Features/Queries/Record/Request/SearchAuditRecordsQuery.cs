using Gise.Audit.Application.Common;
using Gise.Audit.Application.Features.Queries.Record.Response;
using MediatR;

namespace Gise.Audit.Application.Features.Queries.Record.Request;

// Admin: denetim kayıtları, yeniden eskiye, sayfalı. Süzgeçlerin hepsi isteğe bağlı ve birlikte uygulanır (VE).
public sealed class SearchAuditRecordsQuery : IRequest<PagedResult<SearchAuditRecordsResponse>>
{
    // Olay türü: Login, GrantRole, CreateOrder...
    public string? Action { get; set; }

    // success, failure ya da unknown.
    public string? Outcome { get; set; }

    // İşlemi yapan kullanıcının Keycloak kimliği.
    public string? ActorId { get; set; }

    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
