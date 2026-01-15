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

    public async Task<HashSet<string>> GetAllNames(CancellationToken cancellationToken = default)
    {
        var names = await context.Stations
            .Select(s => s.Name)
            .ToListAsync(cancellationToken);

        return names.ToHashSet();
    }

    public async Task<HashSet<string>> GetIdsWhereIsStation(CancellationToken cancellationToken = default)
    {
        var ids = await context.Stations
            .Where(s => s.IsStation)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public void Add(Models.Station station)
    {
        context.Stations.Add(station);
    }
}