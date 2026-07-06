using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;

namespace Watchtower.Application.Features.Reports;

public record GetEndpointCheckStatsQuery(
    Guid EndpointId,
    Guid OwnerId,
    DateTime? From = null,
    DateTime? To = null) : IRequest<CheckStatsDto>;

public record CheckStatsDto(
    Guid EndpointId,
    DateTime From,
    DateTime To,
    int TotalChecks,
    int SuccessfulChecks,
    int FailedChecks,
    double? UptimePercent,
    double? AvgResponseTimeMs,
    long? P95ResponseTimeMs);

public class GetEndpointCheckStatsValidator : AbstractValidator<GetEndpointCheckStatsQuery>
{
    public GetEndpointCheckStatsValidator()
    {
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From < x.To)
            .WithMessage("'From' must be earlier than 'To'.");
    }
}

public class GetEndpointCheckStatsHandler : IRequestHandler<GetEndpointCheckStatsQuery, CheckStatsDto>
{
    private readonly IApplicationDbContext _db;

    public GetEndpointCheckStatsHandler(IApplicationDbContext db) => _db = db;

    public async Task<CheckStatsDto> Handle(GetEndpointCheckStatsQuery request, CancellationToken ct)
    {
        var endpoint = await _db.Endpoints
            .FirstOrDefaultAsync(e => e.Id == request.EndpointId, ct)
            ?? throw new NotFoundException(nameof(MonitoredEndpoint), request.EndpointId);

        if (endpoint.OwnerId != request.OwnerId)
            throw new ForbiddenException();

        var to = request.To ?? DateTime.UtcNow;
        var from = request.From ?? to.AddHours(-24);

        var query = _db.CheckResults
            .Where(c => c.EndpointId == request.EndpointId && c.CheckedAt >= from && c.CheckedAt <= to);

        var totalChecks = await query.CountAsync(ct);

        if (totalChecks == 0)
            return new CheckStatsDto(request.EndpointId, from, to, 0, 0, 0, null, null, null);

        var successfulChecks = await query.CountAsync(c => c.IsSuccess, ct);
        var avgResponseTimeMs = await query.AverageAsync(c => (double)c.ResponseTimeMs, ct);

        // Nearest-rank p95: skip to the value at position ceil(0.95 * n) in ascending order.
        var p95Rank = (int)Math.Ceiling(totalChecks * 0.95);
        var p95ResponseTimeMs = await query
            .OrderBy(c => c.ResponseTimeMs)
            .Skip(p95Rank - 1)
            .Select(c => c.ResponseTimeMs)
            .FirstAsync(ct);

        return new CheckStatsDto(
            request.EndpointId,
            from,
            to,
            totalChecks,
            successfulChecks,
            totalChecks - successfulChecks,
            Math.Round(successfulChecks * 100.0 / totalChecks, 2),
            Math.Round(avgResponseTimeMs, 2),
            p95ResponseTimeMs);
    }
}
