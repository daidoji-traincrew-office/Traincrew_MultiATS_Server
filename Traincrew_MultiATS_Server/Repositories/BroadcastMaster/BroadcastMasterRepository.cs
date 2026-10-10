using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Repositories.BroadcastMaster;

public class BroadcastMasterRepository(ApplicationDbContext context) : IBroadcastMasterRepository
{
    public async Task<BroadcastMasterData> LoadAsync(CancellationToken cancellationToken = default)
    {
        // すべてAsNoTracking。マスタ列だけを読み、*_stateや連動オブジェクトのナビゲーションは読まない
        var stationIds = await context.Stations
            .AsNoTracking()
            .Where(station => station.IsStation)
            .OrderBy(station => station.Id)
            .Select(station => station.Id)
            .ToListAsync(cancellationToken);

        var signalTypes = await context.SignalTypes
            .AsNoTracking()
            .ToDictionaryAsync(type => type.Name, cancellationToken);
        var signals = (await context.Signals
                .AsNoTracking()
                .OrderBy(signal => signal.Name)
                .Select(signal => new
                {
                    signal.Name,
                    signal.TypeName,
                    signal.StationId,
                    signal.TrackCircuitId,
                    signal.DirectionRouteLeftId,
                    signal.DirectionRouteRightId,
                    signal.Direction
                })
                .ToListAsync(cancellationToken))
            .Select(signal => new BroadcastSignalMaster(
                signal.Name,
                signal.TypeName,
                signal.StationId,
                signalTypes[signal.TypeName],
                signal.TrackCircuitId,
                signal.DirectionRouteLeftId,
                signal.DirectionRouteRightId,
                signal.Direction))
            .ToList();

        var nextSignals = await context.NextSignals
            .AsNoTracking()
            .Where(nextSignal => nextSignal.Depth == 1)
            .OrderBy(nextSignal => nextSignal.Id)
            .Select(nextSignal => new { nextSignal.SourceSignalName, nextSignal.TargetSignalName })
            .ToListAsync(cancellationToken);

        var signalRoutes = await context.SignalRoutes
            .AsNoTracking()
            .OrderBy(signalRoute => signalRoute.Id)
            .Select(signalRoute => new BroadcastSignalRouteMaster(signalRoute.SignalName, signalRoute.RouteId))
            .ToListAsync(cancellationToken);

        var displayStations = (await context.TtcWindowDisplayStations
                .AsNoTracking()
                .OrderBy(displayStation => displayStation.id)
                .Select(displayStation => new { displayStation.TtcWindowName, displayStation.StationId })
                .ToListAsync(cancellationToken))
            .ToLookup(displayStation => displayStation.TtcWindowName, displayStation => displayStation.StationId);
        var ttcWindows = (await context.TtcWindows
                .AsNoTracking()
                .OrderBy(ttcWindow => ttcWindow.Name)
                .Select(ttcWindow => new { ttcWindow.Name, ttcWindow.StationId, ttcWindow.Type })
                .ToListAsync(cancellationToken))
            .Select(ttcWindow => new BroadcastTtcWindowMaster(
                ttcWindow.Name,
                ttcWindow.StationId,
                ttcWindow.Type,
                displayStations[ttcWindow.Name].ToList()))
            .ToList();

        var destinationButtons = await context.DestinationButtons
            .AsNoTracking()
            .OrderBy(button => button.Name)
            .Select(button => new BroadcastDestinationButtonMaster(button.Name, button.StationId))
            .ToListAsync(cancellationToken);

        return new(
            stationIds,
            signals,
            nextSignals
                .GroupBy(nextSignal => nextSignal.SourceSignalName)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<string>)group.Select(nextSignal => nextSignal.TargetSignalName).ToList()),
            signalRoutes
                .GroupBy(signalRoute => signalRoute.SignalName)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<ulong>)group.Select(signalRoute => signalRoute.RouteId).ToList()),
            ttcWindows,
            destinationButtons);
    }
}
