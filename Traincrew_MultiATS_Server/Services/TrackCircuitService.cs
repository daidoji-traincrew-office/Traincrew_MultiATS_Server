using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.Services;

public class TrackCircuitService(
    ITrackCircuitRepository trackCircuitRepository,
    IGeneralRepository generalRepository)
{
    public async Task<List<TrackCircuitData>> GetAllTrackCircuitDataList()
    {
        var trackCircuitsDb = await trackCircuitRepository.GetAllTrackCircuitList();
        var trackCircuitDataList = trackCircuitsDb
            .Select(ToTrackCircuitData)
            .ToList();
        return trackCircuitDataList;
    }

    public async Task<List<TrackCircuitData>> GetAllTrackCircuitHiddenDataList()
    {
        var trackCircuitsDb = await trackCircuitRepository.GetAllTrackCircuitList();
        var trackCircuitDataList = trackCircuitsDb
            .Select(ToTrackCircuitDataHidden)
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
        var names = trackCircuitData.Select(t => t.Name).ToList();
        await trackCircuitRepository.SetTrainNumberByNames(names, trainNumber);
    }


    public async Task SetTrackCircuitData(TrackCircuitData trackCircuitData)
    {
        var trackCircuits = await trackCircuitRepository.GetTrackCircuitByName([trackCircuitData.Name]);
        if (trackCircuits.Count == 0)
        {
            // Todo: 例外を吐いたほうが良いとされている
            return;
        }
        var trackCircuitState = trackCircuits[0].TrackCircuitState;
        trackCircuitState.IsLocked = trackCircuitData.Lock;
        trackCircuitState.IsShortCircuit = trackCircuitData.On;
        trackCircuitState.TrainNumber = trackCircuitData.Last;
        await generalRepository.Save(trackCircuitState);
    }

    public async Task ClearTrackCircuitDataList(List<TrackCircuitData> trackCircuitData)
    {
        var names = trackCircuitData.Select(t => t.Name).ToList();
        await trackCircuitRepository.ClearTrainNumberByNames(names);
    }

    public async Task ClearTrackCircuitByTrainNumber(string trainNumber)
    {
        await trackCircuitRepository.ClearTrackCircuitListByTrainNumber(trainNumber);
    }

    public async Task<List<TrackCircuitData>> GetShortCircuitedTrackCircuitDataList()
    {
        return (await trackCircuitRepository.GetWhereShortCircuited())
            .Select(ToTrackCircuitDataHidden)
            .ToList();
    }

    internal static TrackCircuitData ToTrackCircuitData(TrackCircuit trackCircuit)
    {
        return new()
        {
            Last = trackCircuit.TrackCircuitState.TrainNumber,
            Name = trackCircuit.Name,
            On = trackCircuit.TrackCircuitState.IsShortCircuit,
            Lock = trackCircuit.TrackCircuitState.IsLocked
        };
    }

    private static TrackCircuitData ToTrackCircuitDataHidden(TrackCircuit trackCircuit)
    {
        return new()
        {
            Last = string.IsNullOrEmpty(trackCircuit.TrackCircuitState.TrainNumber) ? "" : "溝月レイル",
            Name = trackCircuit.Name,
            On = trackCircuit.TrackCircuitState.IsShortCircuit,
            Lock = trackCircuit.TrackCircuitState.IsLocked
        };
    }
}