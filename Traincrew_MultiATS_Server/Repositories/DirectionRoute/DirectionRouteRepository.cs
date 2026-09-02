using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.DirectionRoute;

public class DirectionRouteRepository(ApplicationDbContext context) : IDirectionRouteRepository
{
    /// <summary>
    /// 全ての方向てこのIDを取得する。
    /// </summary>
    /// <returns>方向てこのIDのリスト。</returns>
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

    /// <summary>
    /// DirectionRoute名からIDへのマッピングを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>DirectionRoute名をキー、IDを値とする辞書</returns>
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

    /// <summary>
    /// 指定されたIDのDirectionRouteを取得する
    /// </summary>
    /// <param name="ids">DirectionRouteのIDリスト</param>
    /// <returns>DirectionRouteのリスト</returns>
    public async Task<List<Models.DirectionRoute>> GetByIds(List<ulong> ids)
    {
        return await context.DirectionRoutes
            .Include(dr => dr.DirectionRouteState)
            .Where(dr => ids.Contains(dr.Id))
            .ToListAsync();
    }

}
