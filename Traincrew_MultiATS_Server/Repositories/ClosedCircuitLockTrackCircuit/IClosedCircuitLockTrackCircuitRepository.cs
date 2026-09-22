namespace Traincrew_MultiATS_Server.Repositories.ClosedCircuitLockTrackCircuit;

public interface IClosedCircuitLockTrackCircuitRepository
{
    /// <summary>
    /// 進路IDから閉路鎖錠対象の軌道回路を取得する
    /// </summary>
    /// <param name="routeIds">進路IDのリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>ClosedCircuitLockTrackCircuitのリスト</returns>
    Task<List<Models.ClosedCircuitLockTrackCircuit>> GetByRouteIds(List<ulong> routeIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 全てのClosedCircuitLockTrackCircuitを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>ClosedCircuitLockTrackCircuitのリスト</returns>
    Task<List<Models.ClosedCircuitLockTrackCircuit>> GetAll(CancellationToken cancellationToken = default);
}
