using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.OperationNotification;

public interface IOperationNotificationRepository
{
    Task<List<OperationNotificationDisplay>> GetAllDisplay();
    Task<List<OperationNotificationDisplay?>> GetDisplayByTrackCircuitIds(List<ulong> trackCircuitIds);
    Task SaveState(OperationNotificationState state);
}
