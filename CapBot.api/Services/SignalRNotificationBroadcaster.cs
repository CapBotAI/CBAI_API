using App.Commons.Interfaces;
using CapBot.api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CapBot.api.Services;

public class SignalRNotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<NotificationHub> _hub;
    public SignalRNotificationBroadcaster(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public Task SendToUserAsync(int userId, string method, object payload, CancellationToken ct = default)
    {
        return _hub.Clients.Group($"user:{userId}").SendAsync(method, payload, ct);
    }
}