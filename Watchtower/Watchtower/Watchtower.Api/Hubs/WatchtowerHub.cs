using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Watchtower.Api.Extensions;

namespace Watchtower.Api.Hubs;

/// <summary>
/// Server-to-client push hub. Each connection joins a per-user group so users
/// only receive events for their own endpoints. Client methods invoked:
/// EndpointStatusChanged, AlertOpened, AlertResolved.
/// </summary>
[Authorize]
public class WatchtowerHub : Hub
{
    public static string UserGroup(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User!.GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }
}
