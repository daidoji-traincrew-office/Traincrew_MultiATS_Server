namespace Traincrew_MultiATS_Server.Repositories.LockConditionByRouteCentralControlLever;

public interface ILockConditionByRouteCentralControlLeverRepository
{
    /// <summary>
    /// 複数の鎖錠条件を一括で追加する
    /// </summary>
    /// <param name="lockConditions">鎖錠条件のリスト</param>
    Task AddAll(List<Models.LockConditionByRouteCentralControlLever> lockConditions);

    /// <summary>
    /// 進路IDのリストから鎖錠条件を取得する
    /// </summary>
    /// <param name="routeIds">進路IDのリスト</param>
    /// <returns>鎖錠条件のリスト</returns>
    Task<List<Models.LockConditionByRouteCentralControlLever>> GetByRouteIds(List<ulong> routeIds);

    /// <summary>
    /// 集中てこIDのリストから鎖錠条件を取得する
    /// </summary>
    /// <param name="routeCentralControlLeverIds">集中てこIDのリスト</param>
    /// <returns>鎖錠条件のリスト</returns>
    Task<List<Models.LockConditionByRouteCentralControlLever>> GetByRouteCentralControlLeverIds(List<ulong> routeCentralControlLeverIds);

    /// <summary>
    /// すべての鎖錠条件を取得する
    /// </summary>
    /// <returns>すべての鎖錠条件のリスト</returns>
    Task<List<Models.LockConditionByRouteCentralControlLever>> GetAll();

    /// <summary>
    /// すべての鎖錠条件を削除する
    /// </summary>
    Task DeleteAll();
}