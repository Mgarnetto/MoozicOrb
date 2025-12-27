using MoozicOrb.Models;

namespace MoozicOrb.Services;

public interface IGroupMessageService
{
    Task SaveGroupMessageAsync(GroupMessage message);
}
