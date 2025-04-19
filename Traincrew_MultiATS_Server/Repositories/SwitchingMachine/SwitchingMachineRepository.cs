using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.SwitchingMachine;

public class SwitchingMachineRepository(ApplicationDbContext context) : ISwitchingMachineRepository
{
    public Task<List<Models.SwitchingMachine>> GetSwitchingMachinesWithState()
    {
        return context.SwitchingMachines.Include(sm => sm.SwitchingMachineState).ToListAsync();
    }

    public async Task<List<ulong>> GetIdsWhereMoving()
    {
        // 1. 転換中の転てつ器
        return await context.SwitchingMachines
            .Include(swm => swm.SwitchingMachineState)
            .Where(swm => swm.SwitchingMachineState.IsSwitching)
            .Select(swm => swm.Id)
            .ToListAsync();
    }
    
    public async Task<List<ulong>> GetIdsWhereLeverReversed()
    {
        // 2. 転てつ器の単独てこが倒れている転てつ器
        return await context.Levers
            .Where(l => l.SwitchingMachineId != null && l.LeverState.IsReversed != LCR.Center)
            .Select(l => l.SwitchingMachineId.Value)
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsWhereLeverRelayRaised()
    {
        // 3. てこリレー回路が上がっている進路に対する転てつ器
        return await context.Routes
            .Include(r => r.RouteState)
            .Where(r => r.RouteState.IsLeverRelayRaised == RaiseDrop.Raise)
            .Join(context.SwitchingMachineRoutes, r => r.Id, smr => smr.RouteId, (r, smr) => smr.SwitchingMachineId)
            .ToListAsync();
    }
}