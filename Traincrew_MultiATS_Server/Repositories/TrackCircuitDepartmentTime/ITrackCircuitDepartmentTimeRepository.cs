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
}
