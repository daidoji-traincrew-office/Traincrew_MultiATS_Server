namespace Traincrew_MultiATS_Server.Repositories.SignalRoute;

public interface ISignalRouteRepository
{
    Task<Dictionary<string, List<Models.Route>>> GetRoutesBySignalNames(IEnumerable<string> signalNames);
}