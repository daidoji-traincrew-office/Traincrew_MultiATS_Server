using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.OperationNotification;

public interface IOperationNotificationRepository
{
    Task<List<OperationNotificationDisplay>> GetAllDisplay();
    Task SaveState(OperationNotificationState state);
}
