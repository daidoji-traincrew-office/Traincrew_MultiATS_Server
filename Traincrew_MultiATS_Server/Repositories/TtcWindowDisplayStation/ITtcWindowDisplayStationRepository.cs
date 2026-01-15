namespace Traincrew_MultiATS_Server.Repositories.TtcWindowDisplayStation;

public interface ITtcWindowDisplayStationRepository
{
    /// <summary>
    /// TtcWindowDisplayStationを追加する
    /// </summary>
    /// <param name="displayStation">追加するTtcWindowDisplayStation</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task AddAsync(Models.TtcWindowDisplayStation displayStation, CancellationToken cancellationToken = default);
}
