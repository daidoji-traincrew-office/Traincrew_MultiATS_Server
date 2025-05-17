using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.DirectionRoute;

public class DirectionRouteRepository(ApplicationDbContext context) : IDirectionRouteRepository
{
    public async Task<List<ulong>> GetAllIds() 
    {
        return await context.DirectionRoutes
            .Select(dl => dl.Id)
            .ToListAsync();
    }

    /// <summary>
    /// すべての DirectionRoute を取得する。
    /// </summary>
    /// <returns>DirectionRoute のリスト。</returns>
    public async Task<List<Models.DirectionRoute>> GetAllWithState()
    {
        return await context.DirectionRoutes
            .Include(route => route.DirectionRouteState)
            .ToListAsync();
    }
}
