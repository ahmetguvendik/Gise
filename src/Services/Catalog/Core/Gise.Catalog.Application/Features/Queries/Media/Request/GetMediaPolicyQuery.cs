using Gise.Catalog.Application.Features.Queries.Media.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Queries.Media.Request;

// Ön yüz dosyayı seçerken tür ve boyutu önceden kontrol eder.
public sealed class GetMediaPolicyQuery : IRequest<GetMediaPolicyResponse>;
