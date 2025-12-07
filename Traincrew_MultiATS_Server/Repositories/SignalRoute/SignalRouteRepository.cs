using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.SignalRoute;

public class SignalRouteRepository(ApplicationDbContext context) : ISignalRouteRepository
{
    public async Task<Dictionary<string, List<Models.Route>>> GetRoutesBySignalNames(IEnumerable<string> signalNames)
    {
        return await context.SignalRoutes
            .Where(sr => signalNames.Contains(sr.SignalName))
            .Include(sr => sr.Route)
            .ThenInclude(r => r.RouteState)
            .Select(sr => new
            {
                sr.SignalName,
                Route = new Models.Route
                {
                    RouteState = sr.Route.RouteState == null ? null : new RouteState
                    {
                        IsSignalControlRaised = sr.Route.RouteState.IsSignalControlRaised
                    }
                }
            })
            .GroupBy(sr => sr.SignalName)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(sr => sr.Route).ToList()
            );
    }

    public async Task<List<Models.SignalRoute>> GetByRouteIds(IEnumerable<ulong> routeIds)
    {
        return await context.SignalRoutes
            .Where(sr => routeIds.Contains(sr.RouteId))
            .ToListAsync();
    }

    public async Task<Dictionary<string, List<Models.Route>>> GetAllRoutes()
    {
        return await context.SignalRoutes
            .Include(sr => sr.Route)
            .ThenInclude(r => r.RouteState)
            .Select(sr => new
            {
                sr.SignalName,
                Route = new Models.Route
                {
                    RouteState = sr.Route.RouteState == null ? null : new RouteState
                    {
                        IsSignalControlRaised = sr.Route.RouteState.IsSignalControlRaised
                    }
                }
            })
            .GroupBy(sr => sr.SignalName)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(sr => sr.Route).ToList()
            );
    }
}