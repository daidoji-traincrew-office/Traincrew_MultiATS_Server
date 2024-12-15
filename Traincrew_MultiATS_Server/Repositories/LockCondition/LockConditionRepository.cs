using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Repositories.LockCondition;

public class LockConditionRepository(ApplicationDbContext context) : ILockConditionRepository
{
    public async Task<List<Models.LockCondition>> GetConditionsByObjectIdAndType(ulong objectId, LockType type)
    {
        return await context.Locks
            .Where(l => l.ObjectId == objectId && l.Type == type)
            .Select(l => l.LockCondition)
            .ToListAsync();
    }
}