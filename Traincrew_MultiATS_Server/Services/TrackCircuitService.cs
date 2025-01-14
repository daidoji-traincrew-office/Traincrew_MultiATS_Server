using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.Services;

public class TrackCircuitService(ITrackCircuitRepository trackCircuitRepository)
{
	public async Task<List<TrackCircuitData>> GetAllTrackCircuitDataList()
	{
		List<TrackCircuitData> trackCircuitDataList = new List<TrackCircuitData>();
		var task = trackCircuitRepository.GetAllTrackCircuitList();
		List<Models.TrackCircuit> trackCircuits_db = task.Result;
		foreach (var item in trackCircuits_db)
		{
			trackCircuitDataList.Add(new TrackCircuitData(){Last = item.TrackCircuitState.TrainNumber, Name = item.Name, On = item.TrackCircuitState.IsShortCircuit});
		}
		return trackCircuitDataList;
	}
}