using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.ThrowOutControl;

public class ThrowOutControlRepository(ApplicationDbContext context) : IThrowOutControlRepository
{
    public async Task<List<Models.ThrowOutControl>> GetAll()
    {
        return await context.ThrowOutControls 
            .ToListAsync();
    }
    
    public async Task<List<Models.ThrowOutControl>> GetBySourceIds(List<ulong> sourceIds)
    {
        return await context.ThrowOutControls
            .Where(t => sourceIds.Contains(t.SourceId))
            .ToListAsync();
    }
    
    public async Task<List<Models.ThrowOutControl>> GetByTargetIds(List<ulong> targetIds)
    {
        return await context.ThrowOutControls
            .Where(t => targetIds.Contains(t.TargetId))
            .ToListAsync();
    }
}