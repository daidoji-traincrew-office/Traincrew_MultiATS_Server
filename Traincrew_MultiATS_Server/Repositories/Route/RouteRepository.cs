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

    public async Task<List<Models.Route>> GetByTcNameWithState(string tcName)
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Where(r => r.TcName == tcName)
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsWhereLeverRelayOrThrowOutIsRaised()
    {
        return await context.Routes
            .Where(r =>
                r.RouteState.IsLeverRelayRaised == RaiseDrop.Raise
                || r.RouteState.IsThrowOutXRRelayRaised == RaiseDrop.Raise
                || r.RouteState.IsThrowOutYSRelayRaised == RaiseDrop.Raise)
            .Select(rs => rs.Id)
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsToOpen()
    {
        return await context.RouteLeverDestinationButtons
            .Where(rldb =>
                rldb.Lever.LeverState.IsReversed == LCR.Left && rldb.Direction == LR.Left
                || rldb.Lever.LeverState.IsReversed == LCR.Right && rldb.Direction == LR.Right
                || rldb.DestinationButton.DestinationButtonState.IsRaised == RaiseDrop.Raise
            )
            .Select(rldb => rldb.RouteId)
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsToOpenWithThrowOutControl()
    {
        return await context.ThrowOutControls
            .Join(context.RouteLeverDestinationButtons,
                toc => toc.SourceId,
                rldb => rldb.RouteId,
                (toc, rldb) => new { toc, source = rldb }
            )
            .Join(context.RouteLeverDestinationButtons,
                combined => combined.toc.TargetId,
                rldb => rldb.RouteId,
                (combined, rldb) => new
                {
                    combined.toc,
                    combined.source,
                    target = rldb
                }
            )
            .Where(combined =>
                (
                    combined.source.Lever.LeverState.IsReversed == LCR.Left && combined.source.Direction == LR.Left
                    || combined.source.Lever.LeverState.IsReversed == LCR.Right && combined.source.Direction == LR.Right
                )
                && (
                    combined.target.DestinationButtonName == null
                    || combined.target.DestinationButton.DestinationButtonState.IsRaised == RaiseDrop.Raise
                )
            )
            .SelectMany(combined => new[] { combined.toc.SourceId, combined.toc.TargetId }.AsEnumerable())
            .ToListAsync();
    }

    public async Task DropRouteRelayWithoutSwitchingMachineWhereLeverRelayIsDropped()
    {
        await context.RouteStates
            .Where(routeState => routeState.IsLeverRelayRaised == RaiseDrop.Drop &&
                                 routeState.IsRouteRelayWithoutSwitchingMachineRaised == RaiseDrop.Raise
            )
            .ExecuteUpdateAsync(r =>
                r.SetProperty(routeState => routeState.IsRouteRelayWithoutSwitchingMachineRaised, RaiseDrop.Drop)
            );
    }

    public async Task DropRouteRelayWhereRouteRelayWithoutSwitchingMachineIsDropped()
    {
        await context.RouteStates
            .Where(routeState => routeState.IsRouteRelayWithoutSwitchingMachineRaised == RaiseDrop.Drop &&
                                 routeState.IsRouteRelayRaised == RaiseDrop.Raise
            )
            .ExecuteUpdateAsync(r => r.SetProperty(routeState => routeState.IsRouteRelayRaised, RaiseDrop.Drop)
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
            .Where(routeState => routeState.IsRouteRelayRaised == RaiseDrop.Drop &&
                                 routeState.IsSignalControlRaised == RaiseDrop.Raise
            )
            .ExecuteUpdateAsync(r => r.SetProperty(routeState => routeState.IsSignalControlRaised, RaiseDrop.Drop)
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

    public async Task<List<ulong>> GetIdsByIdsForSRelay(List<ulong> routeIds)
    {
        return await context.Routes
            .Where(r => routeIds.Contains(r.Id)
                && (r.RouteState.IsThrowOutSRelayRaised == RaiseDrop.Drop
                    || r.RouteState.IsRouteRelayRaised == RaiseDrop.Raise))
            .Select(r => r.Id)
            .ToListAsync();
    }

    public async Task DropThrowOutSRelayExceptByIds(List<ulong> targetIds)
    {
        await context.RouteStates
            .Where(routeState => !targetIds.Contains(routeState.Id) &&
                                 routeState.IsThrowOutSRelayRaised == RaiseDrop.Raise
            )
            .ExecuteUpdateAsync(r =>
                r.SetProperty(routeState => routeState.IsThrowOutSRelayRaised, RaiseDrop.Drop)
            );
    }
}