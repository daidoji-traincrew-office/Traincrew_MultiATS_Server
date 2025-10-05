using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
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
        return await context.SwitchingMachines
            .Join(context.Levers, swm => swm.Id, l => l.SwitchingMachineId, ( swm, l) => new { swm, l })
            .Where(obj => obj.l.LeverState.IsReversed == LCR.Left && obj.swm.SwitchingMachineState.IsReverse != NR.Normal
                || obj.l.LeverState.IsReversed == LCR.Right && obj.swm.SwitchingMachineState.IsReverse != NR.Reversed)
            .Select(obj => obj.swm.Id)
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsWhereLeverRelayRaised()
    {
        // 3. てこリレーが上がっている進路に対する転てつ器
        return await context.Routes
            // てこリレーが扛上している
            .Where(r => r.RouteState.IsLeverRelayRaised == RaiseDrop.Raise)
            // 進路に紐づく転てつ器(転換しないといけない転てつ器)を取得
            .Join(context.SwitchingMachineRoutes, r => r.Id, smr => smr.RouteId, (_, smr) => smr)
            .Join(context.SwitchingMachines, smr => smr.SwitchingMachineId, sm => sm.Id, (smr, sm) => new { smr, sm })
            // 現在の方向が想定方向と違うもの
            .Where(s => s.smr.IsReverse != s.sm.SwitchingMachineState.IsReverse)
            // それらの転てつ器ID
            .Select(s => s.smr.SwitchingMachineId)
            .ToListAsync();
    }
}