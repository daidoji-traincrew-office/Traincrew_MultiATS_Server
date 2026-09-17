namespace Traincrew_MultiATS_Server.Repositories.TrackCircuit;

public interface ITrackCircuitRepository
{
    Task<List<Models.TrackCircuit>> GetAllTrackCircuitList(CancellationToken cancellationToken = default);

    /// <summary>
    /// IDを指定してTrackCircuitStateのみを取得する(TrackCircuit本体はJoinしない)
    /// </summary>
    /// <param name="ids">TrackCircuit ID(=TrackCircuitState ID)のリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>TrackCircuitStateのリスト</returns>
    Task<List<Models.TrackCircuitState>> GetStateByIds(List<ulong> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// IDを指定してTrackCircuitStateのみを取得する(TrackCircuit本体はJoinしない)
    /// </summary>
    /// <param name="id">TrackCircuit ID(=TrackCircuitState ID)</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>TrackCircuitState、存在しない場合はnull</returns>
    Task<Models.TrackCircuitState?> GetStateById(ulong id, CancellationToken cancellationToken = default);

    /// <summary>
    /// すべてのTrackCircuitStateを取得する(TrackCircuit本体はJoinしない)
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>TrackCircuitStateのリスト</returns>
    Task<List<Models.TrackCircuitState>> GetAllStates(CancellationToken cancellationToken = default);

    /// <summary>
    /// 列車番号を指定してTrackCircuitStateのみを取得する(TrackCircuit本体はJoinしない)
    /// </summary>
    /// <param name="trainNumber">列車番号</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>TrackCircuitStateのリスト</returns>
    Task<List<Models.TrackCircuitState>> GetStateByTrainNumber(string trainNumber, CancellationToken cancellationToken = default);
    Task SetTrainNumberByNames(List<string> names, string trainNumber);
    Task ClearTrainNumberByNames(List<string> names);

    /// <summary>
    /// 名前で指定したTrackCircuitのTrainNumber/IsShortCircuit/IsLockedのみを更新する
    /// (track_circuit_stateは2プロセスが別々の列を書くため、LockedBy/UnlockedAtには触れず列限定更新にすること)
    /// </summary>
    /// <param name="name">TrackCircuit名</param>
    /// <param name="trainNumber">列車番号</param>
    /// <param name="isShortCircuit">短絡しているか</param>
    /// <param name="isLocked">鎖錠されているか</param>
    Task SetTrainNumberAndShortCircuitAndLockedByName(
        string name, string trainNumber, bool isShortCircuit, bool isLocked);
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
    /// すべての軌道回路のIDを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>軌道回路IDのリスト</returns>
    Task<List<ulong>> GetAllIds(CancellationToken cancellationToken = default);

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
    Task<Dictionary<string, ulong>> GetAllIdForName(CancellationToken cancellationToken = default);

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
