namespace Traincrew_MultiATS_Server.Repositories.TtcPath;

public interface ITtcPathRepository
{
    Task<List<Models.TtcPath>> GetAll(CancellationToken cancellationToken = default);
}
