namespace Traincrew_MultiATS_Server.Repositories.TrackCircuit;

public interface ITrackCircuitRepository
{
    Task<List<Models.TrackCircuit>> GetAllTrackCircuitList();
    Task<List<Models.TrackCircuit>> GetTrackCircuitListByTrainNumber(string trainNumber);
    Task<List<Models.TrackCircuit>> GetTrackCircuitByName(List<string> trackCircuitNames);
    Task SetTrackCircuitList(List<Models.TrackCircuit> trackCircuitList, string trainNumber);
    Task ClearTrackCircuitList(List<Models.TrackCircuit> trackCircuitList);
    Task ClearTrackCircuitListByTrainNumber(string trainNumber);
}