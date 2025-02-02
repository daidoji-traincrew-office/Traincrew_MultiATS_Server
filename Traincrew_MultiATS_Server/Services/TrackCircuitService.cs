using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.Services;

public class TrackCircuitService(ITrackCircuitRepository trackCircuitRepository)
{
    public async Task<List<TrackCircuitData>> GetAllTrackCircuitDataList()
    {
        List<TrackCircuit> trackCircuitsDb = await trackCircuitRepository.GetAllTrackCircuitList();
        List<TrackCircuitData> trackCircuitDataList = trackCircuitsDb
            .Select(ToTrackCircuitData)
            .ToList();
        return trackCircuitDataList;
    }
    
    public async Task<List<TrackCircuit>> GetTrackCircuitsByNames(List<string> trackCircuitNames)
    {
        return await trackCircuitRepository.GetTrackCircuitByName(trackCircuitNames);
    }
    
    public async Task<List<TrackCircuit>> GetTrackCircuitsByTrainNumber(string trainNumber)
    {
        return await trackCircuitRepository.GetTrackCircuitListByTrainNumber(trainNumber);
    }

    public async Task SetTrackCircuitDataList(List<TrackCircuitData> trackCircuitData, string trainNumber)
    {
        List<TrackCircuit> trackCircuit = trackCircuitData
            .Select(item => new TrackCircuit
            {
                Name = item.Name
            })
            .ToList();
        await trackCircuitRepository.SetTrackCircuitList(trackCircuit, trainNumber);
    }

    public async Task ClearTrackCircuitDataList(List<TrackCircuitData> trackCircuitData)
    {
        List<TrackCircuit> trackCircuit = trackCircuitData
            .Select(item => new TrackCircuit
            {
                Name = item.Name
            })
            .ToList();

        await trackCircuitRepository.ClearTrackCircuitList(trackCircuit);
    }

    internal static TrackCircuitData ToTrackCircuitData(TrackCircuit trackCircuit)
    {
        return new()
        {
            Last = trackCircuit.TrackCircuitState.TrainNumber,
            Name = trackCircuit.Name,
            On = trackCircuit.TrackCircuitState.IsShortCircuit
        };
    }
}