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
}