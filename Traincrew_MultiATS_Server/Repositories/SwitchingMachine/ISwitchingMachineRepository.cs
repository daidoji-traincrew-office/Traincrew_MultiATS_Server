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
    /// てこリレー回路が扛上している進路に対する転てつ器のIDを取得する
    /// </summary>
    Task<List<ulong>> GetIdsWhereLeverRelayRaised();

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
}
