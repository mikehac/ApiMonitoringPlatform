using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Watchtower.Api.Extensions;
using Watchtower.Application.Features.Endpoints;

namespace Watchtower.Api.Controllers;

[ApiController]
[Route("endpoints")]
[Authorize]
public class EndpointsController : ControllerBase
{
    private readonly IMediator _mediator;
    public EndpointsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetEndpointsQuery(User.GetUserId()), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetEndpointByIdQuery(id, User.GetUserId()), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEndpointRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateEndpointCommand(
            User.GetUserId(), req.Name, req.Url, req.HttpMethod,
            req.CheckIntervalSeconds, req.TimeoutSeconds,
            req.ExpectedStatusCode, req.ExpectedBodyContains, req.MaxResponseTimeMs), ct);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEndpointRequest req, CancellationToken ct)
    {
        await _mediator.Send(new UpdateEndpointCommand(
            id, User.GetUserId(), req.Name, req.Url, req.HttpMethod,
            req.CheckIntervalSeconds, req.TimeoutSeconds,
            req.ExpectedStatusCode, req.ExpectedBodyContains, req.MaxResponseTimeMs), ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteEndpointCommand(id, User.GetUserId()), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken ct)
    {
        var isActive = await _mediator.Send(new ToggleEndpointCommand(id, User.GetUserId()), ct);
        return Ok(new { isActive });
    }
}

public record CreateEndpointRequest(
    string Name,
    string Url,
    string HttpMethod,
    int CheckIntervalSeconds,
    int TimeoutSeconds = 30,
    int? ExpectedStatusCode = 200,
    string? ExpectedBodyContains = null,
    int? MaxResponseTimeMs = null);

public record UpdateEndpointRequest(
    string Name,
    string Url,
    string HttpMethod,
    int CheckIntervalSeconds,
    int TimeoutSeconds,
    int? ExpectedStatusCode,
    string? ExpectedBodyContains,
    int? MaxResponseTimeMs);
