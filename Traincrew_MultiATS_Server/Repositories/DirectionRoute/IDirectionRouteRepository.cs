namespace Traincrew_MultiATS_Server.Repositories.DirectionRoute;

public interface IDirectionRouteRepository
{
    /// <summary>
    /// 全ての方向てこのIDを取得する。
    /// </summary>
    /// <returns>方向てこのIDのリスト。</returns>
    Task<List<ulong>> GetAllIds();

    /// <summary>
    /// すべての DirectionRoute を取得する。
    /// </summary>
    /// <returns>DirectionRoute のリスト。</returns>
    Task<List<Models.DirectionRoute>> GetAllWithState();
}
