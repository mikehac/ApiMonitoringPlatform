using FluentValidation;
using MediatR;
using Watchtower.Application.Abstractions;
using Watchtower.Domain.Entities;

namespace Watchtower.Application.Features.Endpoints;

public record CreateEndpointCommand(
    Guid OwnerId,
    string Name,
    string Url,
    string HttpMethod,
    int CheckIntervalSeconds,
    int TimeoutSeconds,
    int? ExpectedStatusCode,
    string? ExpectedBodyContains,
    int? MaxResponseTimeMs) : IRequest<CreateEndpointResult>;

public record CreateEndpointResult(Guid Id, string Name, string Url, string Status);

public class CreateEndpointValidator : AbstractValidator<CreateEndpointCommand>
{
    public CreateEndpointValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048).Must(BeValidUrl).WithMessage("Url must be a valid http/https URL.");
        RuleFor(x => x.HttpMethod).NotEmpty().Must(m => new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD" }.Contains(m.ToUpperInvariant())).WithMessage("HttpMethod must be a valid HTTP verb.");
        RuleFor(x => x.CheckIntervalSeconds).InclusiveBetween(30, 86400);
        RuleFor(x => x.TimeoutSeconds).InclusiveBetween(1, 60);
    }

    private static bool BeValidUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public class CreateEndpointHandler : IRequestHandler<CreateEndpointCommand, CreateEndpointResult>
{
    private readonly IApplicationDbContext _db;

    public CreateEndpointHandler(IApplicationDbContext db) => _db = db;

    public async Task<CreateEndpointResult> Handle(CreateEndpointCommand request, CancellationToken ct)
    {
        var endpoint = MonitoredEndpoint.Create(
            request.OwnerId,
            request.Name,
            request.Url,
            request.HttpMethod,
            request.CheckIntervalSeconds,
            request.TimeoutSeconds,
            request.ExpectedStatusCode,
            request.ExpectedBodyContains,
            request.MaxResponseTimeMs);

        _db.Endpoints.Add(endpoint);
        await _db.SaveChangesAsync(ct);

        return new CreateEndpointResult(endpoint.Id, endpoint.Name, endpoint.Url, endpoint.Status.ToString());
    }
}
