using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Station;

public class StationTimerStateRepository(ApplicationDbContext context) : IStationTimerStateRepository
{
    public async Task<HashSet<(string StationId, int Seconds)>> GetExistingTimerStates(CancellationToken cancellationToken = default)
    {
        var timerStates = await context.StationTimerStates
            .Select(s => new { s.StationId, s.Seconds })
            .ToListAsync(cancellationToken);

        return timerStates.Select(ts => (ts.StationId, ts.Seconds)).ToHashSet();
    }

    public void Add(StationTimerState stationTimerState)
    {
        context.StationTimerStates.Add(stationTimerState);
    }

    /// <summary>
    /// すべてのStationTimerStateのIDを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>StationTimerStateのIDのリスト</returns>
    public async Task<List<ulong>> GetAllIds(CancellationToken cancellationToken = default)
    {
        return await context.StationTimerStates
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
    }
}
