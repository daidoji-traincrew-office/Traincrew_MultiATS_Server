namespace Traincrew_MultiATS_Server.Repositories.TtcWindowTrackCircuit;

public interface ITtcWindowTrackCircuitRepository
{
    /// <summary>
    /// TtcWindowTrackCircuitを追加する
    /// </summary>
    /// <param name="windowTrackCircuit">追加するTtcWindowTrackCircuit</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task AddAsync(Models.TtcWindowTrackCircuit windowTrackCircuit, CancellationToken cancellationToken = default);
}
