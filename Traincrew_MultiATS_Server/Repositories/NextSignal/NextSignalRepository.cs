using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.NextSignal;

public class NextSignalRepository(ApplicationDbContext context): INextSignalRepository
{
    public async Task<List<Models.NextSignal>> GetNextSignalByNamesOrderByDepthDesc(List<string> signalNames)
    {
        return await context.NextSignals
            .Where(s => signalNames.Contains(s.SignalName))
            .OrderByDescending(s => s.Depth)
            .ToListAsync();
    }
}