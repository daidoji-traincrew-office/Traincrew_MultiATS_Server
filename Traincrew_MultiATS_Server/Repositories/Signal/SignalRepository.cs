using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.Signal;

public class SignalRepository(ApplicationDbContext context) : ISignalRepository
{
    public async Task<List<Models.Signal>> GetSignalsByNamesForCalcIndication(List<string> signalNames)
    {
        return await context.Signals
            .Where(s => signalNames.Contains(s.Name))
            .Include(s => s.SignalState)
            .Include(s => s.Type)
            .Include(s => s.TrackCircuit)
            .ThenInclude(t => t!.TrackCircuitState)
            .Include(s => s.Route)
            .ThenInclude(r => r!.RouteState)
            .ToListAsync();
    } 
}