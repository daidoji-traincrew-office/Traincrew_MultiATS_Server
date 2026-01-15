using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;

namespace Traincrew_MultiATS_Server.Repositories.TtcWindowLink;
public class TtcWindowLinkRepository(ApplicationDbContext context) : ITtcWindowLinkRepository
{
    public async Task<List<Models.TtcWindowLink>> GetAllTtcWindowLink()
    {
        return await context.TtcWindowLinks
            .ToListAsync();
    }
    public async Task<List<Models.TtcWindowLink>> GetTtcWindowLinkById(List<ulong> ttcWindowLinkIds)
    {
        return await context.TtcWindowLinks
            .Where(obj => ttcWindowLinkIds.Contains(obj.Id))
            .ToListAsync();
    }
    public async Task<List<Models.TtcWindowLinkRouteCondition>> GetAllTtcWindowLinkRouteConditions()
    {
        return await context.TtcWindowLinkRouteConditions
            .ToListAsync();
    }
    public async Task<List<Models.TtcWindowLinkRouteCondition>> ttcWindowLinkRouteConditionsById(ulong ttcWindowLinkId)
    {
        return await context.TtcWindowLinkRouteConditions
            .Where(obj => obj.TtcWindowLinkId == ttcWindowLinkId)
            .ToListAsync();
    }

    public async Task<HashSet<(string Source, string Target)>> GetAllLinkPairsAsync(CancellationToken cancellationToken = default)
    {
        var links = await context.TtcWindowLinks
            .Select(l => new { l.SourceTtcWindowName, l.TargetTtcWindowName })
            .ToListAsync(cancellationToken);
        return links.Select(l => (l.SourceTtcWindowName, l.TargetTtcWindowName)).ToHashSet();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
