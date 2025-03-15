using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.SignalRoute;

public class SignalRouteRepository(ApplicationDbContext context) : ISignalRouteRepository
{
    public async Task<Dictionary<string, List<Models.Route>>> GetRoutesBySignalNames(IEnumerable<string> signalNames)
    {
        return await context.SignalRoutes
            .Where(sr => signalNames.Contains(sr.SignalName))
            .Include(sr => sr.Route)
            .ThenInclude(r => r.RouteState)
            .GroupBy(sr => sr.SignalName)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(sr => sr.Route).ToList()
            );
    }
}