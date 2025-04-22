using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Station;

public class StationRepository(DbContextOptions<ApplicationDbContext> options, ApplicationDbContext context) : IStationRepository
{
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

    public async Task Save(Models.Station station)
    {
        await using var context = new ApplicationDbContext(options);
        await context.Stations.AddAsync(station);
    }
}