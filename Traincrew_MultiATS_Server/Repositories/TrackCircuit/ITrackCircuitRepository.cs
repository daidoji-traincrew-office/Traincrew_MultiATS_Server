namespace Traincrew_MultiATS_Server.Repositories.TrackCircuit;

public interface ITrackCircuitRepository
{
    Task<List<Models.TrackCircuit>> GetAllTrackCircuitList();
    Task<List<Models.TrackCircuit>> GetTrackCircuitListByTrainNumber(string trainNumber);
    Task<List<Models.TrackCircuit>> GetTrackCircuitByName(List<string> trackCircuitNames);
    Task<List<Models.TrackCircuit>> GetTrackCircuitsById(List<ulong> Ids);
    Task SetTrainNumberByNames(List<string> names, string trainNumber);
    Task ClearTrainNumberByNames(List<string> names);
    Task ClearTrackCircuitListByTrainNumber(string trainNumber);
    Task<List<Models.TrackCircuit>> GetWhereShortCircuited();
    Task LockByIds(List<ulong> ids, ulong routeId);
    Task StartUnlockTimerByIds(List<ulong> ids, DateTime unlockedAt);
    Task UnlockByIds(List<ulong> ids);
    Task<Dictionary<ulong, Models.TrackCircuit>> GetApproachLockFinalTrackCircuitsByRouteIds(List<ulong> routeIds);

    /// <summary>
    /// 全TrackCircuit名を取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>TrackCircuit名のList</returns>
    Task<List<string>> GetAllNames(CancellationToken cancellationToken = default);

    /// <summary>
    /// TrackCircuit名からIDへのマッピングを取得する
    /// </summary>
    /// <param name="trackCircuitNames">TrackCircuit名のリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>TrackCircuit名をキーとしたDictionary</returns>
    Task<Dictionary<string, Models.TrackCircuit>> GetTrackCircuitsByNamesAsync(HashSet<string> trackCircuitNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// 軌道回路名から軌道回路IDへのマッピングを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>軌道回路名をキー、軌道回路IDを値とする辞書</returns>
    Task<Dictionary<string, ulong>> GetAllIdsForName(CancellationToken cancellationToken = default);

    /// <summary>
    /// TrackCircuit名からエンティティへのマッピングを取得する（指定された名前のリストのみ）
    /// </summary>
    /// <param name="trackCircuitNames">取得するTrackCircuit名のリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>TrackCircuit名をキー、TrackCircuitエンティティを値とするDictionary</returns>
    Task<Dictionary<string, Models.TrackCircuit>> GetByNames(List<string> trackCircuitNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// TrackCircuitを更新する
    /// </summary>
    /// <param name="trackCircuit">更新するTrackCircuit</param>
    void Update(Models.TrackCircuit trackCircuit);

    /// <summary>
    /// TrackCircuitエンティティをデタッチする
    /// </summary>
    /// <param name="trackCircuit">デタッチするTrackCircuit</param>
    void Detach(Models.TrackCircuit trackCircuit);
}