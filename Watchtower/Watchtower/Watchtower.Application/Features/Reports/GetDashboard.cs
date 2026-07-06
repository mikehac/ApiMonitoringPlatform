using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;

namespace Watchtower.Application.Features.Reports;

public record GetDashboardQuery(Guid OwnerId) : IRequest<DashboardDto>;

public record DashboardDto(
    int TotalEndpoints,
    int ActiveEndpoints,
    int UpCount,
    int DownCount,
    int DegradedCount,
    int UnknownCount,
    int OpenAlerts,
    List<RecentIncidentDto> RecentIncidents);

public record RecentIncidentDto(
    Guid AlertId,
    Guid EndpointId,
    string EndpointName,
    string State,
    string Reason,
    DateTime TriggeredAt,
    DateTime? ResolvedAt);

public class GetDashboardHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private const int RecentIncidentsCount = 10;

    private readonly IApplicationDbContext _db;

    public GetDashboardHandler(IApplicationDbContext db) => _db = db;

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        var statusCounts = await _db.Endpoints
            .Where(e => e.OwnerId == request.OwnerId)
            .GroupBy(e => new { e.Status, e.IsActive })
            .Select(g => new { g.Key.Status, g.Key.IsActive, Count = g.Count() })
            .ToListAsync(ct);

        var openAlerts = await _db.Alerts
            .CountAsync(a => a.Endpoint.OwnerId == request.OwnerId && a.State == AlertState.Open, ct);

        var recentIncidents = await _db.Alerts
            .Where(a => a.Endpoint.OwnerId == request.OwnerId)
            .OrderByDescending(a => a.TriggeredAt)
            .Take(RecentIncidentsCount)
            .Select(a => new RecentIncidentDto(
                a.Id, a.EndpointId, a.Endpoint.Name,
                a.State.ToString(), a.Reason, a.TriggeredAt, a.ResolvedAt))
            .ToListAsync(ct);

        return new DashboardDto(
            TotalEndpoints: statusCounts.Sum(x => x.Count),
            ActiveEndpoints: statusCounts.Where(x => x.IsActive).Sum(x => x.Count),
            UpCount: statusCounts.Where(x => x.Status == EndpointStatus.Up).Sum(x => x.Count),
            DownCount: statusCounts.Where(x => x.Status == EndpointStatus.Down).Sum(x => x.Count),
            DegradedCount: statusCounts.Where(x => x.Status == EndpointStatus.Degraded).Sum(x => x.Count),
            UnknownCount: statusCounts.Where(x => x.Status == EndpointStatus.Unknown).Sum(x => x.Count),
            OpenAlerts: openAlerts,
            RecentIncidents: recentIncidents);
    }
}
