using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.ClosedCircuitLockTrackCircuit;

public class ClosedCircuitLockTrackCircuitRepository(ApplicationDbContext context)
    : IClosedCircuitLockTrackCircuitRepository
{
    public async Task<List<Models.ClosedCircuitLockTrackCircuit>> GetByRouteIds(List<ulong> routeIds, CancellationToken cancellationToken = default)
    {
        return await context.ClosedCircuitLockTrackCircuits
            .Where(obj => routeIds.Contains(obj.RouteId))
            .OrderBy(obj => obj.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Models.ClosedCircuitLockTrackCircuit>> GetAll(CancellationToken cancellationToken = default)
    {
        return await context.ClosedCircuitLockTrackCircuits
            .ToListAsync(cancellationToken);
    }
}
