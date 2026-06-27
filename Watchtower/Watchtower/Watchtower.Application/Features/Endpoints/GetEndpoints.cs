using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;

namespace Watchtower.Application.Features.Endpoints;

public record GetEndpointsQuery(Guid OwnerId) : IRequest<List<EndpointSummaryDto>>;

public record EndpointSummaryDto(
    Guid Id,
    string Name,
    string Url,
    string HttpMethod,
    string Status,
    DateTime? LastCheckedAt,
    bool IsActive,
    int CheckIntervalSeconds);

public class GetEndpointsHandler : IRequestHandler<GetEndpointsQuery, List<EndpointSummaryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetEndpointsHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<EndpointSummaryDto>> Handle(GetEndpointsQuery request, CancellationToken ct) =>
        await _db.Endpoints
            .Where(e => e.OwnerId == request.OwnerId)
            .OrderBy(e => e.Name)
            .Select(e => new EndpointSummaryDto(
                e.Id, e.Name, e.Url, e.HttpMethod,
                e.Status.ToString(), e.LastCheckedAt, e.IsActive, e.CheckIntervalSeconds))
            .ToListAsync(ct);
}
