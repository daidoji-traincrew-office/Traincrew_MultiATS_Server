using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TtcPathApproachTrackCircuit;

public class TtcPathApproachTrackCircuitRepository(ApplicationDbContext context) : ITtcPathApproachTrackCircuitRepository
{
    public async Task<List<Models.TtcPathApproachTrackCircuit>> GetAll(CancellationToken cancellationToken = default)
    {
        return await context.TtcPathApproachTrackCircuits
            .ToListAsync(cancellationToken);
    }
}
