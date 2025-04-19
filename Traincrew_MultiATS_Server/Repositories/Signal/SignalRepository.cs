using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

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
}