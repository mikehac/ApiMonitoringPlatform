using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;
using Watchtower.Domain.Exceptions;

namespace Watchtower.Application.Features.Endpoints;

public record DeleteEndpointCommand(Guid Id, Guid OwnerId) : IRequest;

public class DeleteEndpointHandler : IRequestHandler<DeleteEndpointCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteEndpointHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteEndpointCommand request, CancellationToken ct)
    {
        var endpoint = await _db.Endpoints
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(MonitoredEndpoint), request.Id);

        if (endpoint.OwnerId != request.OwnerId)
            throw new ForbiddenException();

        _db.Endpoints.Remove(endpoint);
        await _db.SaveChangesAsync(ct);
    }
}
