using Gise.Audit.Application.Common;
using Gise.Audit.Application.Features.Queries.Record.Request;
using Gise.Audit.Application.Features.Queries.Record.Response;

namespace Gise.Audit.Application.Interfaces;

// Denetim kayıtlarının okunduğu yer (Elasticsearch, logs-gise.audit akışı). Yalnızca okur; kayıtları servisler ve
// identity-audit-worker yazar.
public interface IAuditRecordReader
{
    Task<PagedResult<SearchAuditRecordsResponse>> SearchAsync(SearchAuditRecordsQuery query, CancellationToken cancellationToken);
}
