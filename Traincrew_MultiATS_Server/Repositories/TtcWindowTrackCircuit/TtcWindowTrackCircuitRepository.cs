using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TtcWindowTrackCircuit;

public class TtcWindowTrackCircuitRepository(ApplicationDbContext context) : ITtcWindowTrackCircuitRepository
{
    public async Task AddAsync(Models.TtcWindowTrackCircuit windowTrackCircuit, CancellationToken cancellationToken = default)
    {
        await context.TtcWindowTrackCircuits.AddAsync(windowTrackCircuit, cancellationToken);
    }
}
