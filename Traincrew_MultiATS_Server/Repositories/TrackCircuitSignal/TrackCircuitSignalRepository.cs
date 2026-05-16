using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TrackCircuitSignal;

public class TrackCircuitSignalRepository(ApplicationDbContext context) : ITrackCircuitSignalRepository
{
    public async Task<HashSet<(ulong TrackCircuitId, string SignalName)>> GetExistingRelations(
        HashSet<ulong> trackCircuitIds, CancellationToken cancellationToken = default)
    {
        var relations = await context.TrackCircuitSignals
            .Where(tcs => trackCircuitIds.Contains(tcs.TrackCircuitId))
            .Select(tcs => new { tcs.TrackCircuitId, tcs.SignalName })
            .ToListAsync(cancellationToken);

        return relations
            .Select(r => (r.TrackCircuitId, r.SignalName))
            .ToHashSet();
    }
}
