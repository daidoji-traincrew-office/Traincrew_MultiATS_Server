using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.RouteLockTrackCircuit;

public class RouteLockTrackCircuitRepository(ApplicationDbContext context): IRouteLockTrackCircuitRepository
{
    public async Task<List<Models.RouteLockTrackCircuit>> GetByRouteIds(List<ulong> routeIds)
    {
        return await context.RouteLockTrackCircuits
            .Where(obj => routeIds.Contains(obj.RouteId))
            .OrderBy(obj => obj.Id)
            .ToListAsync();
    }
}