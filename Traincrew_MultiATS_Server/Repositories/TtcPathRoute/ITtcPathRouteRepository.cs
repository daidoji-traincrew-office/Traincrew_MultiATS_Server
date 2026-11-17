namespace Traincrew_MultiATS_Server.Repositories.TtcPathRoute;

public interface ITtcPathRouteRepository
{
    Task<List<Models.TtcPathRoute>> GetAll(CancellationToken cancellationToken = default);
}
