namespace Traincrew_MultiATS_Server.Repositories.RouteLockTrackCircuit;

public interface IRouteLockTrackCircuitRepository
{
   Task<List<Models.RouteLockTrackCircuit>> GetByRouteIds(List<ulong> routeIds); 
}