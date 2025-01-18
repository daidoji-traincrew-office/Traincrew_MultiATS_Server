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

    public async Task<List<Models.TrackCircuit>> GetTrackCircuitListByTrainNumber(string trainNumber)
    {
        List<Models.TrackCircuit> trackcircuitlist_db = await context.TrackCircuits
            .Where(odj => odj.TrackCircuitState.TrainNumber == trainNumber)
            .Include(obj => obj.TrackCircuitState).ToListAsync();
        return trackcircuitlist_db;
    }

    public async Task SetTrackCircuitList(List<Models.TrackCircuit> trackCircuitList, string trainNumber)
    {
        foreach (var trackCircuit in trackCircuitList)
        {
            Models.TrackCircuitState item = context.TrackCircuits
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
        foreach (var trackCircuit in trackCircuitList)
        {
            Models.TrackCircuitState item = context.TrackCircuits
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
}