using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.Services;

public interface ITrackCircuitService
{
    Task<List<TrackCircuitData>> GetAllTrackCircuitDataList();
    Task<List<TrackCircuit>> GetTrackCircuitsByNames(List<string> trackCircuitNames);
    Task<List<TrackCircuit>> GetTrackCircuitsByTrainNumber(string trainNumber);
    Task SetTrackCircuitDataList(List<TrackCircuitData> trackCircuitData, string trainNumber);
    Task SetTrackCircuitData(TrackCircuitData trackCircuitData);
    Task ClearTrackCircuitDataList(List<TrackCircuitData> trackCircuitData);
    Task ClearTrackCircuitByTrainNumber(string trainNumber);
    Task<List<TrackCircuitData>> GetShortCircuitedTrackCircuitDataList();
}

public class TrackCircuitService(
    ITrackCircuitRepository trackCircuitRepository,
    IInterlockingObjectMasterStore interlockingObjectMasterStore,
    IGeneralRepository generalRepository) : ITrackCircuitService
{
    public async Task<List<TrackCircuitData>> GetAllTrackCircuitDataList()
    {
        var master = interlockingObjectMasterStore.Current;
        var states = await trackCircuitRepository.GetAllStates();
        return states
            .Select(state =>
                master.TryGetById<TrackCircuit>(state.Id, out var trackCircuit)
                    ? ToTrackCircuitData(trackCircuit, state)
                    : null
            )
            .OfType<TrackCircuitData>()
            .ToList();
    }

    public virtual async Task<List<TrackCircuit>> GetTrackCircuitsByNames(List<string> trackCircuitNames)
    {
        var master = interlockingObjectMasterStore.Current;
        var trackCircuits = master.GetByNames<TrackCircuit>(trackCircuitNames);
        var states = await trackCircuitRepository
            .GetStateByIds(trackCircuits.Select(t => t.Id).ToList());
        return states
            .Select(state =>
                master.TryGetById<TrackCircuit>(state.Id, out var trackCircuit)
                    ? trackCircuit.CloneWithState(state)
                    : null
            )
            .OfType<TrackCircuit>()
            .ToList();
    }

    public async Task<List<TrackCircuit>> GetTrackCircuitsByTrainNumber(string trainNumber)
    {
        var master = interlockingObjectMasterStore.Current;
        var states = await trackCircuitRepository.GetStateByTrainNumber(trainNumber);
        return states
            .Select(state =>
                master.TryGetById<TrackCircuit>(state.Id, out var trackCircuit)
                    ? trackCircuit.CloneWithState(state)
                    : null
            )
            .OfType<TrackCircuit>()
            .ToList();
    }

    public async Task SetTrackCircuitDataList(List<TrackCircuitData> trackCircuitData, string trainNumber)
    {
        if (trackCircuitData.Count == 0)
        {
            return;
        }

        var names = trackCircuitData.Select(t => t.Name).ToList();
        await trackCircuitRepository.SetTrainNumberByNames(names, trainNumber);
    }


    public async Task SetTrackCircuitData(TrackCircuitData trackCircuitData)
    {
        var master = interlockingObjectMasterStore.Current;
        if (!master.TryGetByName<TrackCircuit>(trackCircuitData.Name, out _))
        {
            // Todo: 例外を吐いたほうが良いとされている
            return;
        }
        await trackCircuitRepository.SetTrainNumberAndShortCircuitAndLockedByName(
            trackCircuitData.Name, trackCircuitData.Last, trackCircuitData.On, trackCircuitData.Lock);
    }

    public async Task ClearTrackCircuitDataList(List<TrackCircuitData> trackCircuitData)
    {
        if (trackCircuitData.Count == 0)
        {
            return;
        }

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
            .Select(ToTrackCircuitData)
            .ToList();
    }

    internal static TrackCircuitData ToTrackCircuitData(TrackCircuit trackCircuit)
    {
        return ToTrackCircuitData(trackCircuit, trackCircuit.TrackCircuitState);
    }

    private static TrackCircuitData ToTrackCircuitData(TrackCircuit trackCircuit, TrackCircuitState state) => new()
    {
        Name = trackCircuit.Name,
        Last = state.TrainNumber,
        On = state.IsShortCircuit,
        Lock = state.IsLocked
    };
}