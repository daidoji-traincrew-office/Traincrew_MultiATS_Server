namespace Traincrew_MultiATS_Server.Repositories.SwitchingMachine;

public interface ISwitchingMachineRepository
{
    /// <summary>
    /// 転てつ器とその状態を取得する
    /// </summary>
    Task<List<Models.SwitchingMachine>> GetSwitchingMachinesWithState();

    /// <summary>
    /// 転換中の転てつ器のIDを取得する
    /// </summary>
    Task<List<ulong>> GetIdsWhereMoving();

    /// <summary>
    /// 単独てこが倒れている転てつ器のIDを取得する
    /// </summary>
    Task<List<ulong>> GetIdsWhereLeverReversed();

    /// <summary>
    /// 指定されたIDの転てつ器とその状態を取得する
    /// </summary>
    Task<List<Models.SwitchingMachine>> GetByIdsWithState(List<ulong> ids);

    /// <summary>
    /// すべての転てつ器のIDを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>転てつ器IDのハッシュセット</returns>
    Task<HashSet<ulong>> GetAllIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 転てつ器名から転てつ器IDへのマッピングを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>転てつ器名をキー、転てつ器IDを値とする辞書</returns>
    Task<Dictionary<string, ulong>> GetIdsByNameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// IDを指定してSwitchingMachineStateのみを取得する(SwitchingMachine本体はJoinしない)
    /// </summary>
    /// <param name="ids">SwitchingMachine ID(=SwitchingMachineState ID)のリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>SwitchingMachineStateのリスト</returns>
    Task<List<Models.SwitchingMachineState>> GetStateByIds(IEnumerable<ulong> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 全件のSwitchingMachineStateを取得する。<b>配信(読み取り専用)専用</b>。
    /// </summary>
    /// <remarks>
    /// AsNoTrackingで取得する(追跡しない)。取得した状態を更新・Saveに使ってはならない。
    /// 更新用の取得には既存のGetStateByIds等を使うこと。
    /// </remarks>
    Task<List<Models.SwitchingMachineState>> GetAllStatesForBroadcast(CancellationToken cancellationToken = default);
}
