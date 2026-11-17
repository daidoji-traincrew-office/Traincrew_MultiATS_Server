namespace Traincrew_MultiATS_Server.Repositories.TtcPathApproachTrackCircuit;

public interface ITtcPathApproachTrackCircuitRepository
{
    Task<List<Models.TtcPathApproachTrackCircuit>> GetAll(CancellationToken cancellationToken = default);
}
