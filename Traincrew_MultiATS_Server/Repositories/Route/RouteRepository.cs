using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Route;

public class RouteRepository(ApplicationDbContext context) : IRouteRepository
{
    public async Task<List<Models.Route>> GetByIdsWithState(List<ulong> ids)
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();
    }

    public async Task DropRouteRelayWithoutSwitchingMachineWhereLeverRelayIsDropped()
    {
        await context.RouteStates
            .Where(
                routeState => routeState.IsLeverRelayRaised == RaiseDrop.Drop && routeState.IsRouteRelayWithoutSwitchingMachineRaised == RaiseDrop.Raise
            )
            .ExecuteUpdateAsync(
                r => r.SetProperty(routeState => routeState.IsRouteRelayWithoutSwitchingMachineRaised, RaiseDrop.Drop)
            );
    }

    public async Task DropRouteRelayWhereRouteRelayWithoutSwitchingMachineIsDropped()
    {
        await context.RouteStates
            .Where(
                routeState => routeState.IsRouteRelayWithoutSwitchingMachineRaised == RaiseDrop.Drop && routeState.IsRouteRelayRaised == RaiseDrop.Raise
            )
            .ExecuteUpdateAsync(
                r => r.SetProperty(routeState => routeState.IsRouteRelayRaised, RaiseDrop.Drop)
            );
    }

    public async Task<List<ulong>> GetIdsWhereLeverRelayIsRaised()
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Where(r => r.RouteState.IsLeverRelayRaised == RaiseDrop.Raise)
            .Select(r => r.Id)
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsWhereRouteRelayWithoutSwitchingMachineIsRaised()
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Where(r => r.RouteState.IsRouteRelayWithoutSwitchingMachineRaised == RaiseDrop.Raise)
            .Select(r => r.Id)
            .ToListAsync();
    }

    public async Task DropSignalRelayWhereRouteRelayIsDropped()
    {
        await context.RouteStates
            .Where(
                routeState => routeState.IsRouteRelayRaised == RaiseDrop.Drop && routeState.IsSignalControlRaised == RaiseDrop.Raise
            )
            .ExecuteUpdateAsync(
                r => r.SetProperty(routeState => routeState.IsSignalControlRaised, RaiseDrop.Drop)
            );
    }

    public async Task<List<ulong>> GetIdsWhereRouteRelayIsRaised()
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Where(r => r.RouteState.IsRouteRelayRaised == RaiseDrop.Raise)
            .Select(r => r.Id)
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsForRouteLockRelay()
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Where(r =>
                r.RouteState.IsRouteLockRaised == RaiseDrop.Drop
                || r.RouteState.IsApproachLockMRRaised == RaiseDrop.Drop)
            .Select(r => r.Id)
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsForApproachLockRelay()
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Where(r =>
                r.RouteState.IsRouteRelayRaised == RaiseDrop.Raise
                || r.RouteState.IsApproachLockMRRaised == RaiseDrop.Drop
                || r.RouteState.IsApproachLockMSRaised == RaiseDrop.Raise)
            .Select(r => r.Id)
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsForAll()
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Select(r => r.Id)
            .ToListAsync();
    }

    public async Task<List<Models.Route>> GetWhereApproachLockMSRelayIsRaised()
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Where(r => r.RouteState.IsApproachLockMSRaised == RaiseDrop.Raise)
            .ToListAsync();
    }
}