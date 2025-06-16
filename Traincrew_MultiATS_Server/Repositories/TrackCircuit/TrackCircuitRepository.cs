using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

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
            .Where(obj => Ids.Contains(obj.Id))
            .Include(obj => obj.TrackCircuitState)
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
        // Todo: N+1問題の解消
        foreach (var trackCircuit in trackCircuitList)
        {
            TrackCircuitState item = context.TrackCircuits
                .Include(item => item.TrackCircuitState)
                .Where(obj => obj.Name == trackCircuit.Name)
                .Select(item => item.TrackCircuitState)
                .FirstOrDefault()!;
            item.IsShortCircuit = true;
            item.TrainNumber = trainNumber;
            context.Update(item);
        }
        await context.SaveChangesAsync();
    }

    public async Task ClearTrackCircuitList(List<Models.TrackCircuit> trackCircuitList)
    {
        // Todo: N+1問題の解消
        foreach (var trackCircuit in trackCircuitList)
        {
            TrackCircuitState item = context.TrackCircuits
                .Include(item => item.TrackCircuitState)
                .Where(obj => obj.Name == trackCircuit.Name)
                .Select(item => item.TrackCircuitState)
                .FirstOrDefault()!;
            item.IsShortCircuit = false;
            item.TrainNumber = "";
            context.Update(item);
        }
        await context.SaveChangesAsync();
    }

    public async Task ClearTrackCircuitListByTrainNumber(string trainNumber)
    {
        await context.TrackCircuitStates
            .Where(obj => obj.TrainNumber == trainNumber)
            .ExecuteUpdateAsync(
                item => item
                    .SetProperty(obj => obj.IsShortCircuit, false)
                    .SetProperty(obj => obj.TrainNumber, "")
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