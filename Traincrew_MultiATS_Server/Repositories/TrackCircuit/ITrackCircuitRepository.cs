namespace Traincrew_MultiATS_Server.Repositories.TrackCircuit;

public interface ITrackCircuitRepository
{
    Task<List<Models.TrackCircuit>> GetAllTrackCircuitList();
}