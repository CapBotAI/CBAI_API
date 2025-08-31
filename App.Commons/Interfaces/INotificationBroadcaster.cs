namespace App.Commons.Interfaces;

public interface INotificationBroadcaster
{
    Task SendToUserAsync(int userId, string method, object payload, CancellationToken ct = default);
}