// CapBot.api/Hubs/NotificationHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CapBot.api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId(Context.User);
        if (userId > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId(Context.User);
        if (userId > 0)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnDisconnectedAsync(exception);
    }

    private static int GetUserId(ClaimsPrincipal? user)
    {
        if (user is null) return 0;
        var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? user.FindFirst("id")?.Value
                 ?? user.FindFirst("sub")?.Value;
        return int.TryParse(id, out var uid) ? uid : 0;
    }
}