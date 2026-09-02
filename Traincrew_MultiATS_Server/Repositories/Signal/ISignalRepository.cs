namespace Traincrew_MultiATS_Server.Repositories.Signal;

public interface ISignalRepository
{
    Task<List<Models.Signal>> GetAll();
    Task<List<Models.Signal>> GetSignalsByNamesForCalcIndication(List<string> signalNames);
    Task<List<string>> GetSignalNamesByTrackCircuits(List<string> trackCircuitNames, bool isUp);
    Task<List<string>> GetSignalNamesByStationIds(List<string> stationIds);
    Task<List<Models.Signal>> GetSignalsForCalcIndication();

    /// <summary>
    /// Signal名からSignalエンティティへのマッピングを取得する
    /// </summary>
    /// <param name="signalNames">Signal名のリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>Signal名をキーとしたDictionary</returns>
    Task<Dictionary<string, Models.Signal>> GetSignalsByNamesAsync(HashSet<string> signalNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// 既に登録済みのSignal名を取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>Signal名のHashSet</returns>
    Task<HashSet<string>> GetAllNames(CancellationToken cancellationToken = default);
}