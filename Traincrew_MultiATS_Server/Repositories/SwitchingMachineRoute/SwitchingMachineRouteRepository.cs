using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;

public class SwitchingMachineRouteRepository : ISwitchingMachineRouteRepository
{
    private readonly ApplicationDbContext _context;

    public SwitchingMachineRouteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Models.SwitchingMachineRoute>> GetAll()
    {
        return _context.SwitchingMachineRoutes.ToListAsync();
    }

    public async Task<List<Models.SwitchingMachineRoute>> GetBySwitchingMachineIds(List<ulong> switchingMachineIds)
    {
        return await _context.SwitchingMachineRoutes
            .Where(route => switchingMachineIds.Contains(route.SwitchingMachineId))
            .ToListAsync();
    }
}