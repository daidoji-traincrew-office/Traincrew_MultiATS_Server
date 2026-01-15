namespace Traincrew_MultiATS_Server.Repositories.TrackCircuitSignal;

public interface ITrackCircuitSignalRepository
{
    /// <summary>
    /// 既存の(TrackCircuitId, SignalName, IsUp)のペアを取得する
    /// </summary>
    /// <param name="trackCircuitIds">TrackCircuitIDのリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>(TrackCircuitId, SignalName)のタプルのハッシュセット</returns>
    Task<HashSet<(ulong TrackCircuitId, string SignalName)>> GetExistingRelations(
        HashSet<ulong> trackCircuitIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// TrackCircuitSignalを追加する
    /// </summary>
    /// <param name="trackCircuitSignal">追加するTrackCircuitSignal</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task AddAsync(Models.TrackCircuitSignal trackCircuitSignal, CancellationToken cancellationToken = default);
}
