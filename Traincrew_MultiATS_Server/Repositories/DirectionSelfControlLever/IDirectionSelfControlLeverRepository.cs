namespace Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;

public interface IDirectionSelfControlLeverRepository
{
    /// <summary>
    /// 開放てこを名前から取得する。
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Task<Models.DirectionSelfControlLever?> GetDirectionSelfControlLeverByNameWithState(string name);

    /// <summary>
    /// 全ての開放てこのIDを取得する。
    /// </summary>
    /// <returns>開放てこのIDのリスト。</returns>
    Task<List<ulong>> GetAllIds();
}
