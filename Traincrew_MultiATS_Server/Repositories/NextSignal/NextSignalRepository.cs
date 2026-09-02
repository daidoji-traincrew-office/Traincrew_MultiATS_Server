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

    public async Task<List<Models.NextSignal>> GetByNamesAndDepth(List<string> signalNames, int depth)
    {
        return await context.NextSignals
            .Where(s => signalNames.Contains(s.SignalName) && s.Depth == depth)
            .ToListAsync();
    }

    public async Task<List<Models.NextSignal>> GetAllByDepth(int depth)
    {
        return await context.NextSignals
            .Where(s => s.Depth == depth)
            .ToListAsync();
    }

    public async Task<List<Models.NextSignal>> GetByNamesAndMaxDepthOrderByDepth(List<string> signalNames, int depth)
    {
        return await context.NextSignals
            .Where(s => signalNames.Contains(s.SignalName) && s.Depth <= depth)
            .OrderBy(s => s.Depth)
            .ToListAsync();
    }

    public async Task<List<Models.NextSignal>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.NextSignals.ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, List<string>>> GetByDepthGroupedBySignalNameAsync(int depth, CancellationToken cancellationToken = default)
    {
        return await context.NextSignals
            .Where(ns => ns.Depth == depth)
            .GroupBy(ns => ns.SignalName)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(ns => ns.TargetSignalName).ToList(),
                cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}