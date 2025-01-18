using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.Services;

public class TrackCircuitService(ITrackCircuitRepository trackCircuitRepository)
{
	public async Task<List<TrackCircuitData>> GetAllTrackCircuitDataList()
	{
		List<TrackCircuitData> trackCircuitDataList = new List<TrackCircuitData>();
		List<Models.TrackCircuit> trackCircuits_db = await trackCircuitRepository.GetAllTrackCircuitList();;
		trackCircuitDataList.AddRange
		(
			trackCircuits_db.Select(item => new TrackCircuitData()
			{
				Last = item.TrackCircuitState.TrainNumber,
				Name = item.Name,
				On = item.TrackCircuitState.IsShortCircuit
			})
		);
		return trackCircuitDataList;
	}
	public async Task<List<TrackCircuitData>> GetTrackCircuitDataListByTrainNumber(string trainNumber)
	{
		List<TrackCircuitData> trackCircuitDataList = new List<TrackCircuitData>();
		List<Models.TrackCircuit> trackCircuits_db = await trackCircuitRepository.GetTrackCircuitListByTrainNumber(trainNumber);
		trackCircuitDataList.AddRange
		(
			trackCircuits_db.Select(item => new TrackCircuitData()
			{
				Last = item.TrackCircuitState.TrainNumber,
				Name = item.Name,
				On = item.TrackCircuitState.IsShortCircuit
			})
		);
		return trackCircuitDataList;
	}
}