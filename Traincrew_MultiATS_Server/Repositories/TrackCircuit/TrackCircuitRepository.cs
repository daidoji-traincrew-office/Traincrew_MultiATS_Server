using System.Net.Http.Headers;
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
			Models.TrackCircuit item = context.TrackCircuits
				.FirstOrDefault(obj => obj.Name == trackCircuit.Name)!;
			item.TrackCircuitState.TrainNumber = trainNumber;
			item.TrackCircuitState.IsShortCircuit = true;
		}
		await context.SaveChangesAsync();
	}

	public async Task ClearTrackCircuitList(List<Models.TrackCircuit> trackCircuitList)
	{
		foreach (var trackCircuit in trackCircuitList)
		{
			Models.TrackCircuit item = context.TrackCircuits
				.FirstOrDefault(obj => obj.Name == trackCircuit.Name)!;
			item.TrackCircuitState.TrainNumber = "";
			item.TrackCircuitState.IsShortCircuit = false;
		}
		await context.SaveChangesAsync();
	}
}