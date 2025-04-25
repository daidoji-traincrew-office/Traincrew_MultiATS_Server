namespace Traincrew_MultiATS_Server.Repositories.DirectionLever;

public interface IDirectionLeverRepository
{
    /// <summary>
    /// 全ての方向てこのIDを取得する。
    /// </summary>
    /// <returns>方向てこのIDのリスト。</returns>
    Task<List<ulong>> GetAllIds();
}
