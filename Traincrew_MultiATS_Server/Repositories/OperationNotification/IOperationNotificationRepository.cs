namespace Traincrew_MultiATS_Server.Repositories.OperationNotification;

public interface IOperationNotificationRepository
{
    Task<List<Models.OperationNotificationState>> GetAllStates();
    /// <summary>
    /// 解除/取消のうち、指定時刻以前に操作されたものを「なし」に戻す。
    /// </summary>
    /// <returns>実際に更新された行数。0ならキャッシュを無効化する必要がない</returns>
    Task<int> SetNoneWhereKaijoOrTorikeshiAndOperatedBeforeOrEqual(DateTime operatedAt);
}
