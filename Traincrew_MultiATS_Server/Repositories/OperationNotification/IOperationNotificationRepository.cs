namespace Traincrew_MultiATS_Server.Repositories.OperationNotification;

public interface IOperationNotificationRepository
{
    Task<List<Models.OperationNotificationDisplay>> GetAllDisplay();
    Task<List<Models.OperationNotificationDisplay?>> GetDisplayByTrackCircuitIds(List<ulong> trackCircuitIds);
    Task SetNoneWhereKaijoOrTorikeshiAndOperatedBeforeOrEqual(DateTime operatedAt);

    /// <summary>
    /// 告知器名 -> 紐づく軌道回路 ID 一覧。マスタデータなので変化しない。
    /// </summary>
    Task<Dictionary<string, List<ulong>>> GetTrackCircuitIdsByDisplayName();

    /// <summary>
    /// 軌道回路 ID -> 告知器名。マスタデータなので変化しない。
    /// </summary>
    Task<Dictionary<ulong, string>> GetDisplayNameByTrackCircuitId();

    /// <summary>
    /// 全告知器の状態。
    /// </summary>
    Task<List<Models.OperationNotificationState>> GetAllStates();
}
