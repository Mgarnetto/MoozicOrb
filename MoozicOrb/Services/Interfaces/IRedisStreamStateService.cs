namespace MoozicOrb.Services.Interfaces;

public interface IRedisStreamStateService
{
    Task AddListenerAsync(string streamId, int userId);
    Task RemoveListenerAsync(string streamId, int userId);
    Task<bool> IsListenerAsync(string streamId, int userId);

    Task SetBroadcasterAsync(string streamId, int userId);
    Task<int?> GetBroadcasterAsync(string streamId);

    Task RefreshTTLAsync(string streamId);
}
