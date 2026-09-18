namespace Traincrew_MultiATS_Server.Repositories.OperationNotification;

public interface IOperationNotificationRepository
{
    Task<Models.OperationNotificationState?> GetStateByDisplayName(string displayName);
    Task<List<Models.OperationNotificationState>> GetAllStates();
    Task SetNoneWhereKaijoOrTorikeshiAndOperatedBeforeOrEqual(DateTime operatedAt);
}
