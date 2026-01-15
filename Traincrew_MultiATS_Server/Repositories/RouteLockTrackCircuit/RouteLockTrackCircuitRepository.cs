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

    public async Task<List<(ulong RouteId, ulong TrackCircuitId)>> GetAllPairs(CancellationToken cancellationToken = default)
    {
        return await context.RouteLockTrackCircuits
            .Select(r => new ValueTuple<ulong, ulong>(r.RouteId, r.TrackCircuitId))
            .ToListAsync(cancellationToken);
    }

    public void Add(Models.RouteLockTrackCircuit routeLockTrackCircuit)
    {
        context.RouteLockTrackCircuits.Add(routeLockTrackCircuit);
    }
}