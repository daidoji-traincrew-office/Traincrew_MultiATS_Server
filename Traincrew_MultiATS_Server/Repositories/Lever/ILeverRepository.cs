namespace Traincrew_MultiATS_Server.Repositories.Lever;

public interface ILeverRepository
{
    Task<List<ulong>> GetIdsBySwitchingMachineIds(List<ulong> ids);

    Task<Models.Lever?> GetLeverByNameWithState(string name);

    Task<List<ulong>?> GetAllIds();
    
    Task<List<Models.Lever>> GetAllWithState();
    
    Task<List<Models.Lever>> GetByIdsWithState(IEnumerable<ulong> ids);

    /// <summary>
    /// IDを指定してLeverStateのみを取得する(Lever本体はJoinしない)
    /// </summary>
    /// <param name="ids">Lever ID(=LeverState ID)のリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>LeverStateのリスト</returns>
    Task<List<Models.LeverState>> GetStateByIds(IEnumerable<ulong> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 全件のLeverStateを取得する。<b>配信(読み取り専用)専用</b>。
    /// </summary>
    /// <remarks>
    /// AsNoTrackingで取得する(追跡しない)。取得した状態を更新・Saveに使ってはならない。
    /// 更新用の取得には既存のGetStateByIds等を使うこと。
    /// </remarks>
    Task<List<Models.LeverState>> GetAllStatesForBroadcast(CancellationToken cancellationToken = default);
}
