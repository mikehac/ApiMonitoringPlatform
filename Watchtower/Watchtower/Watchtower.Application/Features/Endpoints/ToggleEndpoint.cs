using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;
using Watchtower.Domain.Exceptions;

namespace Watchtower.Application.Features.Endpoints;

public record ToggleEndpointCommand(Guid Id, Guid OwnerId) : IRequest<bool>;

public class ToggleEndpointHandler : IRequestHandler<ToggleEndpointCommand, bool>
{
    private readonly IApplicationDbContext _db;

    public ToggleEndpointHandler(IApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(ToggleEndpointCommand request, CancellationToken ct)
    {
        var endpoint = await _db.Endpoints
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(MonitoredEndpoint), request.Id);

        if (endpoint.OwnerId != request.OwnerId)
            throw new ForbiddenException();

        if (endpoint.IsActive) endpoint.Deactivate(); else endpoint.Activate();
        await _db.SaveChangesAsync(ct);

        return endpoint.IsActive;
    }
}
