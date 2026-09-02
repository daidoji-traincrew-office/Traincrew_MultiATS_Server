using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Station;

public class StationRepository(ApplicationDbContext context) : IStationRepository
{

    public async Task<List<Models.Station>> GetWhereIsStation()
    {
        return await context.Stations
            .Where(s => s.IsStation)
            .ToListAsync();
    }

    public Task<Models.Station?> GetStationById(string id)
    {
        return context.Stations.FirstOrDefaultAsync(s => s.Id == id);
    }

    public Task<Models.Station?> GetStationByName(string name)
    {
        return context.Stations.FirstOrDefaultAsync(s => s.Name == name);
    }

    public Task<List<Models.Station>> GetStationByIds(IEnumerable<string> ids)
    {
        return context.Stations
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();
    }

    public async Task<List<StationTimerState>> GetAllTimerStates()
    {
        return await context.StationTimerStates
            .ToListAsync();
    }

    public async Task<List<StationTimerState>> GetTimerStatesByStationIds(IEnumerable<string> stationIds)
    {
        return await context.StationTimerStates
            .Where(s => stationIds.Contains(s.StationId))
            .ToListAsync();
    }

    /// <summary>
    /// 指定したIDのStationTimerStateのIsTimerConditionMetのみを更新する
    /// (station_timer_stateは2プロセスが別々の列を書くため、列限定更新にすること)
    /// </summary>
    /// <param name="ids">StationTimerStateのIDリスト</param>
    /// <param name="isTimerConditionMet">時素条件成立フラグ</param>
    public async Task SetIsTimerConditionMetByIds(List<ulong> ids, bool isTimerConditionMet)
    {
        await context.StationTimerStates
            .Where(s => ids.Contains(s.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.IsTimerConditionMet, isTimerConditionMet));
    }

    public async Task<List<string>> GetAllNames(CancellationToken cancellationToken = default)
    {
        return await context.Stations
            .Select(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetIdsWhereIsStation(CancellationToken cancellationToken = default)
    {
        return await context.Stations
            .Where(s => s.IsStation)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    public void Add(Models.Station station)
    {
        context.Stations.Add(station);
    }
}