using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Signal;

public class SignalRepository(ApplicationDbContext context) : ISignalRepository
{
    public async Task<List<Models.Signal>> GetAll()
    {
        return await context.Signals.ToListAsync();
    }

    public async Task<List<Models.Signal>> GetSignalsByNamesForCalcIndication(List<string> signalNames)
    {
        return await context.Signals
            .Where(s => signalNames.Contains(s.Name))
            .Include(s => s.SignalState)
            .Include(s => s.Type)
            .Include(s => s.TrackCircuit)
            .ThenInclude(t => t!.TrackCircuitState)
            .Include(s => s.DirectionRouteLeft)
            .ThenInclude(dr => dr.DirectionRouteState)
            .Include(s => s.DirectionRouteRight)
            .ThenInclude(dr => dr.DirectionRouteState)
            .Select(s => new Models.Signal
            {
                Name = s.Name,
                SignalState = s.SignalState,
                Direction = s.Direction,
                DirectionRouteLeftId = s.DirectionRouteLeftId,
                DirectionRouteRightId = s.DirectionRouteRightId,
                Type = s.Type,
                TrackCircuit = s.TrackCircuit == null ? null : new Models.TrackCircuit
                {
                    Name = s.TrackCircuit.Name,
                    TrackCircuitState = new()
                    {
                        IsShortCircuit = s.TrackCircuit.TrackCircuitState.IsShortCircuit,
                        TrainNumber = s.TrackCircuit.TrackCircuitState.TrainNumber, 
                    }
                },
                DirectionRouteLeft = s.DirectionRouteLeft == null ? null : new Models.DirectionRoute
                {
                    DirectionRouteState = s.DirectionRouteLeft.DirectionRouteState == null ? null : new DirectionRouteState
                    {
                        isLr = s.DirectionRouteLeft.DirectionRouteState.isLr
                    }
                },
                DirectionRouteRight = s.DirectionRouteRight == null ? null : new Models.DirectionRoute
                {
                    DirectionRouteState = s.DirectionRouteRight.DirectionRouteState == null ? null : new DirectionRouteState
                    {
                        isLr = s.DirectionRouteRight.DirectionRouteState.isLr
                    }
                }
            })
            .ToListAsync();
    }

    public async Task<List<string>> GetSignalNamesByTrackCircuits(List<string> trackCircuitNames, bool isUp)
    {
        return await context.TrackCircuitSignals
            .Where(tcs => trackCircuitNames.Contains(tcs.TrackCircuit.Name) && tcs.IsUp == isUp)
            .Select(tcs => tcs.SignalName)
            .ToListAsync();
    }

    public async Task<List<string>> GetSignalNamesByStationIds(List<string> stationIds)
    {
        return await context.Signals
            .Where(signal => stationIds.Contains(signal.StationId))
            .Select(signal => signal.Name)
            .ToListAsync();
    }

    public async Task<List<Models.Signal>> GetSignalsForCalcIndication()
    {
        return await context.Signals
            .Include(s => s.SignalState)
            .Include(s => s.Type)
            .Include(s => s.TrackCircuit)
            .ThenInclude(t => t!.TrackCircuitState)
            .Include(s => s.DirectionRouteLeft)
            .ThenInclude(dr => dr.DirectionRouteState)
            .Include(s => s.DirectionRouteRight)
            .ThenInclude(dr => dr.DirectionRouteState)
            .Select(s => new Models.Signal
            {
                Name = s.Name,
                SignalState = s.SignalState,
                Direction = s.Direction,
                DirectionRouteLeftId = s.DirectionRouteLeftId,
                DirectionRouteRightId = s.DirectionRouteRightId,
                Type = s.Type,
                TrackCircuit = s.TrackCircuit == null ? null : new Models.TrackCircuit
                {
                    Name = s.TrackCircuit.Name,
                    TrackCircuitState = new()
                    {
                        IsShortCircuit = s.TrackCircuit.TrackCircuitState.IsShortCircuit,
                        TrainNumber = s.TrackCircuit.TrackCircuitState.TrainNumber,
                    }
                },
                DirectionRouteLeft = s.DirectionRouteLeft == null ? null : new Models.DirectionRoute
                {
                    DirectionRouteState = s.DirectionRouteLeft.DirectionRouteState == null ? null : new DirectionRouteState
                    {
                        isLr = s.DirectionRouteLeft.DirectionRouteState.isLr
                    }
                },
                DirectionRouteRight = s.DirectionRouteRight == null ? null : new Models.DirectionRoute
                {
                    DirectionRouteState = s.DirectionRouteRight.DirectionRouteState == null ? null : new DirectionRouteState
                    {
                        isLr = s.DirectionRouteRight.DirectionRouteState.isLr
                    }
                }
            })
            .ToListAsync();
    }
}