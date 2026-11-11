using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.LockConditionByRouteCentralControlLever;

public class LockConditionByRouteCentralControlLeverRepository(ApplicationDbContext context) : ILockConditionByRouteCentralControlLeverRepository
{
    public async Task AddAll(List<Models.LockConditionByRouteCentralControlLever> lockConditions)
    {
        context.LockConditionByRouteCentralControlLevers.AddRange(lockConditions);
        await context.SaveChangesAsync();
    }

    public async Task<List<Models.LockConditionByRouteCentralControlLever>> GetByRouteIds(List<ulong> routeIds)
    {
        return await context.LockConditionByRouteCentralControlLevers
            .Where(lcbrcl => routeIds.Contains(lcbrcl.RouteId))
            .ToListAsync();
    }

    public async Task<List<Models.LockConditionByRouteCentralControlLever>> GetByRouteCentralControlLeverIds(List<ulong> routeCentralControlLeverIds)
    {
        return await context.LockConditionByRouteCentralControlLevers
            .Where(lcbrcl => routeCentralControlLeverIds.Contains(lcbrcl.RouteCentralControlLeverId))
            .ToListAsync();
    }

    public Task<List<Models.LockConditionByRouteCentralControlLever>> GetAll() => GetAll(CancellationToken.None);

    /// <summary>読み取りをキャンセル可能な形で設備情報を取得する。</summary>
    public async Task<List<Models.LockConditionByRouteCentralControlLever>> GetAll(CancellationToken cancellationToken)
    {
        return await context.LockConditionByRouteCentralControlLevers
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAll()
    {
        await context.LockConditionByRouteCentralControlLevers.ExecuteDeleteAsync();
    }
}