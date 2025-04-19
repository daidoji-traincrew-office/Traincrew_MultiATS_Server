using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Lock;

public class LockRepository(ApplicationDbContext dbContext) : ILockRepository
{
    public async Task<Dictionary<ulong, List<Models.Lock>>> GetByObjectIdsAndType(List<ulong> objectIds, LockType type)
    {
        return await dbContext.Locks
            .Where(l => objectIds.Contains(l.ObjectId) && l.Type == type)
            .Include(l => l.LockConditions)
            .GroupBy(l => l.ObjectId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.OrderBy(l => l.Id)
                    .ThenBy(lc => lc.Id)
                    .ToList());
    }
}