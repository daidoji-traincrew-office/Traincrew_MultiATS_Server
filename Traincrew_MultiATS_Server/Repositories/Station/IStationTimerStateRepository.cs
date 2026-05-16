using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Station;

public interface IStationTimerStateRepository
{
    /// <summary>
    /// 既存のStationTimerStateの(StationId, Seconds)組み合わせを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>既存の(StationId, Seconds)組み合わせのHashSet</returns>
    Task<HashSet<(string StationId, int Seconds)>> GetExistingTimerStates(CancellationToken cancellationToken = default);

    /// <summary>
    /// StationTimerStateを追加する
    /// </summary>
    /// <param name="stationTimerState">追加するStationTimerState</param>
    void Add(StationTimerState stationTimerState);
}
