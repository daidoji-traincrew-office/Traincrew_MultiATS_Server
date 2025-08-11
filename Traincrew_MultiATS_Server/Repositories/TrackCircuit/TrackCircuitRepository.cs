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

    public async Task SetTrackCircuitList(List<Models.TrackCircuit> trackCircuitList, string trainNumber)
    {
        await context.TrackCircuits
            .Where(tc => trackCircuitList.Contains(tc))
            .Select(tc => tc.TrackCircuitState)
            .ExecuteUpdateAsync(item => item
                .SetProperty(tcs => tcs.IsShortCircuit, true)
                .SetProperty(tcs => tcs.TrainNumber, trainNumber));
    }

    public async Task ClearTrackCircuitList(List<Models.TrackCircuit> trackCircuitList)
    {
        await context.TrackCircuits
            .Where(tc => trackCircuitList.Contains(tc))
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
}