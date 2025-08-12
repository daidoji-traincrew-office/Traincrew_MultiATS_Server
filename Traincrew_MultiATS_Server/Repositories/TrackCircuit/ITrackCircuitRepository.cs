namespace Traincrew_MultiATS_Server.Repositories.TrackCircuit;

public interface ITrackCircuitRepository
{
    Task<List<Models.TrackCircuit>> GetAllTrackCircuitList();
    Task<List<Models.TrackCircuit>> GetTrackCircuitListByTrainNumber(string trainNumber);
    Task<List<Models.TrackCircuit>> GetTrackCircuitByName(List<string> trackCircuitNames);
    Task<List<Models.TrackCircuit>> GetTrackCircuitsById(List<ulong> Ids);
    Task SetTrainNumberByNames(List<string> names, string trainNumber);
    Task ClearTrainNumberByNames(List<string> names);
    Task ClearTrackCircuitListByTrainNumber(string trainNumber);
    Task<List<Models.TrackCircuit>> GetWhereShortCircuited();
    Task LockFromRouteByIds(List<Models.TrackCircuit> trackCircuitList, ulong routeId);
    Task StartUnlockTimerByIds(List<Models.TrackCircuit> trackCircuitList, DateTime unlockedAt);
    Task UnlockByIds(List<Models.TrackCircuit> trackCircuitList);
}