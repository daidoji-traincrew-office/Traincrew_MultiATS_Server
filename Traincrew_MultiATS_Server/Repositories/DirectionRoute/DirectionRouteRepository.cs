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

    public async Task<Dictionary<string, ulong>> GetIdsByNameAsync(CancellationToken cancellationToken = default)
    {
        return await context.DirectionRoutes
            .Select(dr => new { dr.Name, dr.Id })
            .ToDictionaryAsync(dr => dr.Name, dr => dr.Id, cancellationToken);
    }

    /// <summary>
    /// DirectionRoute名からDirectionRouteエンティティへのマッピングを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>DirectionRoute名をキー、DirectionRouteエンティティを値とする辞書</returns>
    public async Task<Dictionary<string, Models.DirectionRoute>> GetByNamesAsDictionaryAsync(CancellationToken cancellationToken = default)
    {
        return await context.DirectionRoutes
            .ToDictionaryAsync(dr => dr.Name, cancellationToken);
    }

    /// <summary>
    /// DirectionRouteを更新する
    /// </summary>
    /// <param name="directionRoute">更新するDirectionRoute</param>
    public void Update(Models.DirectionRoute directionRoute)
    {
        context.DirectionRoutes.Update(directionRoute);
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
