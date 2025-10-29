using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.RouteCentralControlLever;

public class RouteCentralControlLeverRepository(ApplicationDbContext context) : IRouteCentralControlLeverRepository
{
    /// <summary>
    /// 進路集中制御てこを名前から取得する。
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public async Task<Models.RouteCentralControlLever?> GetByNameWithState(string name)
    {
        return await context.RouteCentralControlLevers
            .Include(lever => lever.RouteCentralControlLeverState)
            .FirstOrDefaultAsync(lever => lever.Name == name);
    }

    /// <summary>
    /// 全ての進路集中制御てこのIDを取得する。
    /// </summary>
    /// <returns>進路集中制御てこのIDのリスト。</returns>
    public async Task<List<ulong>> GetAllIds()
    {
        return await context.RouteCentralControlLevers
            .Select(l => l.Id)
            .ToListAsync();
    }

    /// <summary>
    /// すべての RouteCentralControlLever を取得する。
    /// </summary>
    /// <returns>RouteCentralControlLever のリスト。</returns>
    public async Task<List<Models.RouteCentralControlLever>> GetAllWithState()
    {
        return await context.RouteCentralControlLevers
            .Include(lever => lever.RouteCentralControlLeverState)
            .ToListAsync();
    }

    /// <summary>
    /// 指定されたIDのリストから進路集中制御てこを取得する。
    /// </summary>
    /// <param name="ids">進路集中制御てこのIDのリスト。</param>
    /// <returns>RouteCentralControlLever のリスト。</returns>
    public async Task<List<Models.RouteCentralControlLever>> GetByIds(List<ulong> ids)
    {
        return await context.RouteCentralControlLevers
            .Include(lever => lever.RouteCentralControlLeverState)
            .Where(lever => ids.Contains(lever.Id))
            .ToListAsync();
    }
}