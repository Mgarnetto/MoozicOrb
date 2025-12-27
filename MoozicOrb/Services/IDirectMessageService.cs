using MoozicOrb.Models;

namespace MoozicOrb.Services;

public interface IDirectMessageService
{
    Task SaveDirectMessageAsync(DirectMessage message);
}
