using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;
using Watchtower.Application.Common;

namespace Watchtower.Application.Features.Reports;

public record GetEndpointAlertsQuery(
    Guid EndpointId,
    Guid OwnerId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<AlertDto>>;

public record AlertDto(
    Guid Id,
    string State,
    string Reason,
    DateTime TriggeredAt,
    DateTime? ResolvedAt,
    double? DurationSeconds);

public class GetEndpointAlertsValidator : AbstractValidator<GetEndpointAlertsQuery>
{
    public GetEndpointAlertsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetEndpointAlertsHandler : IRequestHandler<GetEndpointAlertsQuery, PagedResult<AlertDto>>
{
    private readonly IApplicationDbContext _db;

    public GetEndpointAlertsHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<AlertDto>> Handle(GetEndpointAlertsQuery request, CancellationToken ct)
    {
        var endpoint = await _db.Endpoints
            .FirstOrDefaultAsync(e => e.Id == request.EndpointId, ct)
            ?? throw new NotFoundException(nameof(MonitoredEndpoint), request.EndpointId);

        if (endpoint.OwnerId != request.OwnerId)
            throw new ForbiddenException();

        var query = _db.Alerts.Where(a => a.EndpointId == request.EndpointId);

        var totalCount = await query.CountAsync(ct);

        var alerts = await query
            .OrderByDescending(a => a.TriggeredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = alerts
            .Select(a => new AlertDto(
                a.Id,
                a.State.ToString(),
                a.Reason,
                a.TriggeredAt,
                a.ResolvedAt,
                a.ResolvedAt is { } resolved ? (resolved - a.TriggeredAt).TotalSeconds : null))
            .ToList();

        return new PagedResult<AlertDto>(items, request.Page, request.PageSize, totalCount);
    }
}
