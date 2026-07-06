using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;
using Watchtower.Application.Common;

namespace Watchtower.Application.Features.Reports;

public record GetEndpointChecksQuery(
    Guid EndpointId,
    Guid OwnerId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<CheckResultDto>>;

public record CheckResultDto(
    Guid Id,
    DateTime CheckedAt,
    bool IsSuccess,
    int? StatusCode,
    long ResponseTimeMs,
    string? ErrorMessage);

public class GetEndpointChecksValidator : AbstractValidator<GetEndpointChecksQuery>
{
    public GetEndpointChecksValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetEndpointChecksHandler : IRequestHandler<GetEndpointChecksQuery, PagedResult<CheckResultDto>>
{
    private readonly IApplicationDbContext _db;

    public GetEndpointChecksHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<CheckResultDto>> Handle(GetEndpointChecksQuery request, CancellationToken ct)
    {
        var endpoint = await _db.Endpoints
            .FirstOrDefaultAsync(e => e.Id == request.EndpointId, ct)
            ?? throw new NotFoundException(nameof(MonitoredEndpoint), request.EndpointId);

        if (endpoint.OwnerId != request.OwnerId)
            throw new ForbiddenException();

        var query = _db.CheckResults.Where(c => c.EndpointId == request.EndpointId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CheckedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CheckResultDto(
                c.Id, c.CheckedAt, c.IsSuccess, c.StatusCode, c.ResponseTimeMs, c.ErrorMessage))
            .ToListAsync(ct);

        return new PagedResult<CheckResultDto>(items, request.Page, request.PageSize, totalCount);
    }
}
