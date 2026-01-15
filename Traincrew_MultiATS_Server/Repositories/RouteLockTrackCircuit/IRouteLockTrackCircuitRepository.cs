namespace Traincrew_MultiATS_Server.Repositories.RouteLockTrackCircuit;

public interface IRouteLockTrackCircuitRepository
{
    Task<List<Models.RouteLockTrackCircuit>> GetByRouteIds(List<ulong> routeIds);

    /// <summary>
    /// 既存の(RouteId, TrackCircuitId)ペアを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>(RouteId, TrackCircuitId)の匿名型オブジェクトのリスト</returns>
    Task<List<(ulong RouteId, ulong TrackCircuitId)>> GetAllPairs(CancellationToken cancellationToken = default);

    /// <summary>
    /// RouteLockTrackCircuitを追加する
    /// </summary>
    /// <param name="routeLockTrackCircuit">追加するRouteLockTrackCircuit</param>
    void Add(Models.RouteLockTrackCircuit routeLockTrackCircuit);
}