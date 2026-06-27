using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;
using Watchtower.Domain.Exceptions;

namespace Watchtower.Application.Features.Endpoints;

public record GetEndpointByIdQuery(Guid Id, Guid OwnerId) : IRequest<EndpointDetailDto>;

public record EndpointDetailDto(
    Guid Id,
    string Name,
    string Url,
    string HttpMethod,
    int CheckIntervalSeconds,
    int TimeoutSeconds,
    int? ExpectedStatusCode,
    string? ExpectedBodyContains,
    int? MaxResponseTimeMs,
    string Status,
    DateTime? LastCheckedAt,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class GetEndpointByIdHandler : IRequestHandler<GetEndpointByIdQuery, EndpointDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetEndpointByIdHandler(IApplicationDbContext db) => _db = db;

    public async Task<EndpointDetailDto> Handle(GetEndpointByIdQuery request, CancellationToken ct)
    {
        var e = await _db.Endpoints
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(MonitoredEndpoint), request.Id);

        if (e.OwnerId != request.OwnerId)
            throw new ForbiddenException();

        return new EndpointDetailDto(
            e.Id, e.Name, e.Url, e.HttpMethod,
            e.CheckIntervalSeconds, e.TimeoutSeconds,
            e.ExpectedStatusCode, e.ExpectedBodyContains, e.MaxResponseTimeMs,
            e.Status.ToString(), e.LastCheckedAt, e.IsActive,
            e.CreatedAt, e.UpdatedAt);
    }
}
