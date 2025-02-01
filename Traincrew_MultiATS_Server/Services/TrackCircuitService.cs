using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.Services;

public class TrackCircuitService(ITrackCircuitRepository trackCircuitRepository)
{
    public async Task<List<int>> GetBougoZoneByTrackCircuitDataList(List<TrackCircuitData> trackCircuitDataList)
    {
        List<Models.TrackCircuit> trackCircuit = trackCircuitDataList
            .Select(x => new Models.TrackCircuit
            {
                Name = x.Name
            }).ToList();
        return await trackCircuitRepository.GetBougoZoneByTrackCircuitList(trackCircuit);
    }
    public async Task<List<TrackCircuitData>> GetAllTrackCircuitDataList()
    {
        List<Models.TrackCircuit> trackCircuitsDb = await trackCircuitRepository.GetAllTrackCircuitList();
        List<TrackCircuitData> trackCircuitDataList = trackCircuitsDb
            .Select(ToTrackCircuitData)
            .ToList();
        return trackCircuitDataList;
    }

    public async Task<List<TrackCircuitData>> GetTrackCircuitDataListByTrainNumber(string trainNumber)
    {
        List<Models.TrackCircuit> trackCircuitsDb =
            await trackCircuitRepository.GetTrackCircuitListByTrainNumber(trainNumber);
        List<TrackCircuitData> trackCircuitDataList = trackCircuitsDb
            .Select(ToTrackCircuitData)
            .ToList();
        return trackCircuitDataList;
    }

    public async Task SetTrackCircuitDataList(List<TrackCircuitData> trackCircuitData, string trainNumber)
    {
        List<Models.TrackCircuit> trackCircuit = trackCircuitData
            .Select(item => new Models.TrackCircuit
            {
                Name = item.Name
            })
            .ToList();
        await trackCircuitRepository.SetTrackCircuitList(trackCircuit, trainNumber);
    }

    public async Task ClearTrackCircuitDataList(List<TrackCircuitData> trackCircuitData)
    {
        List<Models.TrackCircuit> trackCircuit = trackCircuitData
            .Select(item => new Models.TrackCircuit
            {
                Name = item.Name
            })
            .ToList();

        await trackCircuitRepository.ClearTrackCircuitList(trackCircuit);
    }

    private static TrackCircuitData ToTrackCircuitData(Models.TrackCircuit trackCircuit)
    {
        return new()
        {
            Last = trackCircuit.TrackCircuitState.TrainNumber,
            Name = trackCircuit.Name,
            On = trackCircuit.TrackCircuitState.IsShortCircuit
        };
    }
}