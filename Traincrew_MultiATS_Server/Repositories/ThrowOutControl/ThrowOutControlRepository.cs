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
    
    public async Task<List<Models.ThrowOutControl>> GetBySourceRouteIds(List<ulong> sourceRouteIds)
    {
        return await context.ThrowOutControls
            .Where(t => sourceRouteIds.Contains(t.SourceRouteId))
            .ToListAsync();
    }
    
    public async Task<List<Models.ThrowOutControl>> GetByTargetRouteIds(List<ulong> targetRouteIds)
    {
        return await context.ThrowOutControls
            .Where(t => targetRouteIds.Contains(t.TargetRouteId))
            .ToListAsync();
    }
}