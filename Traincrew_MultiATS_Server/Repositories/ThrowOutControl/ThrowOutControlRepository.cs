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

    public async Task<List<Models.ThrowOutControl>> GetBySourceIdsAndTypes(List<ulong> sourceIds, List<ThrowOutControlType> controlTypes)
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

    public async Task<List<Models.ThrowOutControl>> GetByTargetIdsAndTypes(List<ulong> targetIds, List<ThrowOutControlType> controlTypes)
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

    /// <summary>
    /// すべてのThrowOutControlのSourceIdとTargetIdのペアを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>SourceIdとTargetIdのペアのHashSet</returns>
    public async Task<HashSet<(ulong SourceId, ulong TargetId)>> GetAllPairsAsync(CancellationToken cancellationToken = default)
    {
        var pairs = await context.ThrowOutControls
            .Select(toc => new { toc.SourceId, toc.TargetId })
            .ToListAsync(cancellationToken);
        return pairs.Select(p => (p.SourceId, p.TargetId)).ToHashSet();
    }

    /// <summary>
    /// ThrowOutControlを追加する
    /// </summary>
    /// <param name="throwOutControl">追加するThrowOutControl</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public async Task AddAsync(Models.ThrowOutControl throwOutControl, CancellationToken cancellationToken = default)
    {
        await context.ThrowOutControls.AddAsync(throwOutControl, cancellationToken);
    }

    /// <summary>
    /// 変更を保存する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}