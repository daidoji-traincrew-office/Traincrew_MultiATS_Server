namespace Traincrew_MultiATS_Server.Repositories.TtcPathPredecessor;

public interface ITtcPathPredecessorRepository
{
    Task<List<Models.TtcPathPredecessor>> GetAll(CancellationToken cancellationToken = default);
}
