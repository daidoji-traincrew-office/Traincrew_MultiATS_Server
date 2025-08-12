using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TrackCircuit;

public class TrackCircuitRepository(ApplicationDbContext context) : ITrackCircuitRepository
{
    public async Task<List<Models.TrackCircuit>> GetAllTrackCircuitList()
    {
        List<Models.TrackCircuit> trackcircuitlist_db = await context.TrackCircuits
            .Include(obj => obj.TrackCircuitState).ToListAsync();
        return trackcircuitlist_db;
    }

    public async Task<List<Models.TrackCircuit>> GetTrackCircuitByName(List<string> trackCircuitNames)
    {
        return await context.TrackCircuits
            .Where(obj => trackCircuitNames.Contains(obj.Name))
            .Include(obj => obj.TrackCircuitState)
            .ToListAsync();
    }

    public async Task<List<Models.TrackCircuit>> GetTrackCircuitsById(List<ulong> Ids)
    {
        return await context.TrackCircuits
            .Where(tc => Ids.Contains(tc.Id))
            .Include(tc => tc.TrackCircuitState)
            .ToListAsync();
    }

    public async Task<List<Models.TrackCircuit>> GetTrackCircuitListByTrainNumber(string trainNumber)
    {
        List<Models.TrackCircuit> trackcircuitlist_db = await context.TrackCircuits
            .Where(odj => odj.TrackCircuitState.TrainNumber == trainNumber)
            .Include(obj => obj.TrackCircuitState).ToListAsync();
        return trackcircuitlist_db;
    }

    public async Task SetTrainNumberByNames(List<string> names, string trainNumber)
    {
        await context.TrackCircuits
            .Where(trackCircuit => names.Contains(trackCircuit.Name))
            .Select(tc => tc.TrackCircuitState)
            .ExecuteUpdateAsync(item => item
                .SetProperty(tcs => tcs.IsShortCircuit, true)
                .SetProperty(tcs => tcs.TrainNumber, trainNumber));
    }

    public async Task ClearTrainNumberByNames(List<string> names)
    {
        await context.TrackCircuits
            .Where(trackCircuit => names.Contains(trackCircuit.Name))
            .Select(tc => tc.TrackCircuitState)
            .ExecuteUpdateAsync(item => item
                .SetProperty(tcs => tcs.IsShortCircuit, false)
                .SetProperty(tcs => tcs.TrainNumber, ""));
    }

    public async Task ClearTrackCircuitListByTrainNumber(string trainNumber)
    {
        await context.TrackCircuitStates
            .Where(tcs => tcs.TrainNumber == trainNumber)
            .ExecuteUpdateAsync(item => item
                .SetProperty(tcs => tcs.IsShortCircuit, false)
                .SetProperty(tcs => tcs.TrainNumber, "")
            );
    }

    public async Task<List<Models.TrackCircuit>> GetWhereShortCircuited()
    {
        return await context.TrackCircuits
            .Include(tc => tc.TrackCircuitState)
            .Where(tc => tc.TrackCircuitState.IsShortCircuit)
            .ToListAsync();
    }

    public async Task LockFromRouteByIds(List<Models.TrackCircuit> trackCircuitList, ulong routeId)
    {
        await context.TrackCircuits
            .Where(tc => trackCircuitList.Select(trackCircuit => trackCircuit.Id).Contains(tc.Id))
            .Select(tc => tc.TrackCircuitState)
            .ExecuteUpdateAsync(item => item
                .SetProperty(tcs => tcs.IsLocked, true)
                .SetProperty(tcs => tcs.LockedBy, routeId));
    }

    public async Task StartUnlockTimerByIds(List<Models.TrackCircuit> trackCircuitList, DateTime unlockedAt)
    {
        await context.TrackCircuits
            .Where(tc => trackCircuitList.Select(trackCircuit => trackCircuit.Id).Contains(tc.Id))
            .Select(tc => tc.TrackCircuitState)
            .ExecuteUpdateAsync(item => item
                .SetProperty(tcs => tcs.UnlockedAt, unlockedAt));
    }

    public async Task UnlockByIds(List<Models.TrackCircuit> trackCircuitList)
    {
        await context.TrackCircuits
            .Where(tc => trackCircuitList.Select(trackCircuit => trackCircuit.Id).Contains(tc.Id))
            .Select(tc => tc.TrackCircuitState)
            .ExecuteUpdateAsync(item => item
                .SetProperty(tcs => tcs.IsLocked, false)
                .SetProperty(tcs => tcs.LockedBy, (ulong?)null)
                .SetProperty(tcs => tcs.UnlockedAt, (DateTime?)null));
    }
}