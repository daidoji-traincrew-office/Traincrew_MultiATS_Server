namespace Traincrew_MultiATS_Server.Repositories.RouteCentralControlLever;

public interface IRouteCentralControlLeverRepository
{
    /// <summary>
    /// 進路集中制御てこを名前から取得する。
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Task<Models.RouteCentralControlLever?> GetByNameWithState(string name);

    /// <summary>
    /// 全ての進路集中制御てこのIDを取得する。
    /// </summary>
    /// <returns>進路集中制御てこのIDのリスト。</returns>
    Task<List<ulong>> GetAllIds();

    /// <summary>
    /// すべての RouteCentralControlLever を取得する。
    /// </summary>
    /// <returns>RouteCentralControlLever のリスト。</returns>
    Task<List<Models.RouteCentralControlLever>> GetAllWithState();

    /// <summary>
    /// 指定されたIDのリストから進路集中制御てこを取得する。
    /// </summary>
    /// <param name="ids">進路集中制御てこのIDのリスト。</param>
    /// <returns>RouteCentralControlLever のリスト。</returns>
    Task<List<Models.RouteCentralControlLever>> GetByIds(List<ulong> ids);
}