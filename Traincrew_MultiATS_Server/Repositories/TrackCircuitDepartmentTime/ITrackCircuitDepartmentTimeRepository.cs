namespace Traincrew_MultiATS_Server.Repositories.TrackCircuitDepartmentTime;

public interface ITrackCircuitDepartmentTimeRepository
{
    /// <summary>
    /// 軌道回路IDと上り下りと両数以下の最大両数を持つ出発時素を取得する
    /// </summary>
    /// <param name="trackCircuitId">軌道回路ID</param>
    /// <param name="isUp">上りかどうか</param>
    /// <param name="maxCarCount">最大両数</param>
    /// <returns>出発時素情報</returns>
    Task<Models.TrackCircuitDepartmentTime?> GetByTrackCircuitIdAndIsUpAndMaxCarCount(ulong trackCircuitId, bool isUp, int maxCarCount);

    /// <summary>
    /// 指定された軌道回路IDのリストに対応する出発時素を取得する
    /// </summary>
    /// <param name="trackCircuitIds">軌道回路IDのリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>出発時素情報のリスト</returns>
    Task<List<Models.TrackCircuitDepartmentTime>> GetByTrackCircuitIds(List<ulong> trackCircuitIds, CancellationToken cancellationToken = default);
}
