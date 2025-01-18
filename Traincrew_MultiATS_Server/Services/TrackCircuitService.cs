using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.Services;

public class TrackCircuitService(ITrackCircuitRepository trackCircuitRepository)
{
	public async Task<List<TrackCircuitData>> GetAllTrackCircuitDataList()
	{
		List<Models.TrackCircuit> trackCircuits_db = await trackCircuitRepository.GetAllTrackCircuitList();;
		List<TrackCircuitData> trackCircuitDataList = 
			trackCircuits_db.Select(item => new TrackCircuitData()
			{
				Last = item.TrackCircuitState.TrainNumber,
				Name = item.Name,
				On = item.TrackCircuitState.IsShortCircuit
			}).ToList();
		return trackCircuitDataList;
	}
	public async Task<List<TrackCircuitData>> GetTrackCircuitDataListByTrainNumber(string trainNumber)
	{
		List<Models.TrackCircuit> trackCircuits_db = await trackCircuitRepository.GetTrackCircuitListByTrainNumber(trainNumber);
		List<TrackCircuitData> trackCircuitDataList = 
			trackCircuits_db.Select(item => new TrackCircuitData()
			{
				Last = item.TrackCircuitState.TrainNumber,
				Name = item.Name,
				On = item.TrackCircuitState.IsShortCircuit
			}).ToList();
		return trackCircuitDataList;
	}

	public async Task SetTrackCircuitDataList(List<TrackCircuitData> trackCircuitData, string trainNumber)
	{
		List<Models.TrackCircuit> trackCircuit = new List<Models.TrackCircuit>();
		foreach (var item in trackCircuitData)
		{
			trackCircuit.Add(new Models.TrackCircuit()
			{
				Name = item.Name,
				TrackCircuitState = new Models.TrackCircuitState()
				{
					TrainNumber = trainNumber,
					IsShortCircuit = true
				}
			});
		}
		await trackCircuitRepository.SetTrackCircuitList(trackCircuit, trainNumber);
	}
	public async Task ClearTrackCircuitDataList(List<TrackCircuitData> trackCircuitData)
	{
		List<Models.TrackCircuit> trackCircuit = new List<Models.TrackCircuit>();
		foreach (var item in trackCircuitData)
		{
			trackCircuit.Add(new Models.TrackCircuit()
			{
				Name = item.Name,
				TrackCircuitState = new Models.TrackCircuitState()
				{
					TrainNumber = "",
					IsShortCircuit = false
				}
			});
		}
		await trackCircuitRepository.ClearTrackCircuitList(trackCircuit);
	}
}