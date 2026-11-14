using Gise.Audit.Application.Common;
using Gise.Audit.Application.Features.Queries.Record.Request;
using Gise.Audit.Application.Features.Queries.Record.Response;
using Gise.Audit.Application.Interfaces;
using MediatR;

namespace Gise.Audit.Application.Features.Handlers.Record.QueryHandlers;

public sealed class SearchAuditRecordsQueryHandler : IRequestHandler<SearchAuditRecordsQuery, PagedResult<SearchAuditRecordsResponse>>
{
    private readonly IAuditRecordReader _reader;

    public SearchAuditRecordsQueryHandler(IAuditRecordReader reader)
    {
        _reader = reader;
    }

    public Task<PagedResult<SearchAuditRecordsResponse>> Handle(SearchAuditRecordsQuery request, CancellationToken cancellationToken)
        => _reader.SearchAsync(request, cancellationToken);
}
