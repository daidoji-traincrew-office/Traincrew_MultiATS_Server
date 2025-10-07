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
    Task LockByIds(List<ulong> ids, ulong routeId);
    Task StartUnlockTimerByIds(List<ulong> ids, DateTime unlockedAt);
    Task UnlockByIds(List<ulong> ids);
    Task<Dictionary<ulong, Models.TrackCircuit>> GetApproachLockFinalTrackCircuitsByRouteIds(List<ulong> routeIds);
}