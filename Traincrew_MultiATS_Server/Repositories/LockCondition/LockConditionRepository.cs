using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.LockCondition;

public class LockConditionRepository(ApplicationDbContext context) : ILockConditionRepository
{
    /*
    public async Task<List<Models.LockCondition>> GetConditionsByObjectIdAndType(ulong objectId, LockType type)
    {
        return await context.Locks
            .Where(l => l.ObjectId == objectId && l.Type == type)
            .Select(l => l.LockCondition)
            .ToListAsync();
    }
    */
    public async Task<Dictionary<ulong, List<Models.LockCondition>>> GetConditionsByType(LockType type)
    {
        return await context.Locks
            .Where(l => l.Type == type)
            .Join(context.LockConditions, l => l.Id, lc => lc.LockId, (l, lc) => new { l.ObjectId, lc })
            .GroupBy(x => x.ObjectId)
            .ToDictionaryAsync(
                x => x.Key, 
                x => x.Select(y => y.lc).OrderBy(lc => lc.Id).ToList());
    }

    public async Task<Dictionary<ulong, List<Models.LockCondition>>> GetConditionsByObjectIdsAndType(List<ulong> objectIds, LockType type)
    {
        return await context.Locks
            .Where(l => objectIds.Contains(l.ObjectId) && l.Type == type)
            .Join(context.LockConditions, l => l.Id, lc => lc.LockId, (l, lc) => new { l.ObjectId, lc })
            .GroupBy(x => x.ObjectId)
            .ToDictionaryAsync(
                x => x.Key,
                x => x.Select(y => y.lc).OrderBy(lc => lc.Id).ToList());
    }

    public async Task DeleteAll()
    {
        await context.LockConditions.ExecuteDeleteAsync();
    }
}