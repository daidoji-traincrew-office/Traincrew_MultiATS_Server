namespace Traincrew_MultiATS_Server.Repositories.OperationNotification;

public interface IOperationNotificationRepository
{
    Task<List<Models.OperationNotificationDisplay>> GetAllDisplay();
    Task<List<Models.OperationNotificationDisplay?>> GetDisplayByTrackCircuitIds(List<ulong> trackCircuitIds);
    Task SetNoneWhereKaijoOrTorikeshiAndOperatedBeforeOrEqual(DateTime operatedAt);
}
