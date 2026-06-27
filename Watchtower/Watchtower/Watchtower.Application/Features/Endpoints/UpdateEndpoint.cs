using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;
using Watchtower.Domain.Exceptions;

namespace Watchtower.Application.Features.Endpoints;

public record UpdateEndpointCommand(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Url,
    string HttpMethod,
    int CheckIntervalSeconds,
    int TimeoutSeconds,
    int? ExpectedStatusCode,
    string? ExpectedBodyContains,
    int? MaxResponseTimeMs) : IRequest;

public class UpdateEndpointValidator : AbstractValidator<UpdateEndpointCommand>
{
    public UpdateEndpointValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme == "http" || u.Scheme == "https"))
            .WithMessage("Url must be a valid http/https URL.");
        RuleFor(x => x.HttpMethod).NotEmpty()
            .Must(m => new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD" }.Contains(m.ToUpperInvariant()))
            .WithMessage("HttpMethod must be a valid HTTP verb.");
        RuleFor(x => x.CheckIntervalSeconds).InclusiveBetween(30, 86400);
        RuleFor(x => x.TimeoutSeconds).InclusiveBetween(1, 60);
    }
}

public class UpdateEndpointHandler : IRequestHandler<UpdateEndpointCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateEndpointHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateEndpointCommand request, CancellationToken ct)
    {
        var endpoint = await _db.Endpoints
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(MonitoredEndpoint), request.Id);

        if (endpoint.OwnerId != request.OwnerId)
            throw new ForbiddenException();

        endpoint.UpdateSettings(
            request.Name, request.Url, request.HttpMethod,
            request.CheckIntervalSeconds, request.TimeoutSeconds,
            request.ExpectedStatusCode, request.ExpectedBodyContains, request.MaxResponseTimeMs);

        await _db.SaveChangesAsync(ct);
    }
}
