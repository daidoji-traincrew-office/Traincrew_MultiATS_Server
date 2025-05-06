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
}