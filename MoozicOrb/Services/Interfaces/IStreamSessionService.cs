namespace MoozicOrb.Services.Interfaces;

public interface IStreamSessionService
{
    Task RegisterConnectionAsync(
        string streamId,
        int userId,
        string connectionId,
        bool isBroadcaster
    );

    Task RemoveConnectionAsync(string streamId, int userId);
    Task RemoveConnectionByConnectionIdAsync(string connectionId);
    Task RefreshHeartbeatAsync(string streamId, int userId);
}

