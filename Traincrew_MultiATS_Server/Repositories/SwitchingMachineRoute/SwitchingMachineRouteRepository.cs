using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;

public class SwitchingMachineRouteRepository(ApplicationDbContext context) : ISwitchingMachineRouteRepository
{
    public Task<List<Models.SwitchingMachineRoute>> GetAll()
    {
        return context.SwitchingMachineRoutes.ToListAsync();
    }

    public async Task<List<Models.SwitchingMachineRoute>> GetBySwitchingMachineIds(List<ulong> switchingMachineIds)
    {
        return await context.SwitchingMachineRoutes
            .Where(route => switchingMachineIds.Contains(route.SwitchingMachineId))
            .ToListAsync();
    }

    public async Task<Dictionary<ulong, Models.SwitchingMachineRoute?>> GetFirstByRouteIds(
        List<ulong> routeIds)
    {
        return await context.SwitchingMachineRoutes
            .Where(smr => routeIds.Contains(smr.RouteId))
            .OrderBy(smr => smr.Id)
            .GroupBy(smr => smr.RouteId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.FirstOrDefault()
            );
    }

    public async Task DeleteAll()
    {
        await context.SwitchingMachineRoutes.ExecuteDeleteAsync();
    }

    public async Task<HashSet<(ulong RouteId, ulong SwitchingMachineId)>> GetAllPairsAsync(CancellationToken cancellationToken = default)
    {
        var pairs = await context.SwitchingMachineRoutes
            .Select(smr => new { smr.RouteId, smr.SwitchingMachineId })
            .ToListAsync(cancellationToken);
        return pairs.Select(p => (p.RouteId, p.SwitchingMachineId)).ToHashSet();
    }
}