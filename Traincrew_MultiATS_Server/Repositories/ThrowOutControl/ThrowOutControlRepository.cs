using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

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

    public async Task<List<Models.ThrowOutControl>> GetBySourceIds(List<ulong> sourceIds, List<ThrowOutControlType> controlTypes)
    {
        return await context.ThrowOutControls
            .Where(t => sourceIds.Contains(t.SourceId) && controlTypes.Contains(t.ControlType))
            .ToListAsync();
    }

    public async Task<List<Models.ThrowOutControl>> GetByTargetIds(List<ulong> targetIds)
    {
        return await context.ThrowOutControls
            .Where(t => targetIds.Contains(t.TargetId))
            .ToListAsync();
    }

    public async Task<List<Models.ThrowOutControl>> GetByTargetIds(List<ulong> targetIds, List<ThrowOutControlType> controlTypes)
    {
        return await context.ThrowOutControls
            .Where(t => targetIds.Contains(t.TargetId) && controlTypes.Contains(t.ControlType))
            .ToListAsync();
    }

    public async Task<List<Models.ThrowOutControl>> GetBySourceAndTargetIds(List<ulong> ids)
    {
        return await context.ThrowOutControls
            .Where(t => ids.Contains(t.SourceId) || ids.Contains(t.TargetId))
            .ToListAsync();

    }

    public async Task<List<Models.ThrowOutControl>> GetByControlTypes(List<ThrowOutControlType> controlTypes)
    {
        return await context.ThrowOutControls
            .Where(t => controlTypes.Contains(t.ControlType))
            .ToListAsync();
    }
}