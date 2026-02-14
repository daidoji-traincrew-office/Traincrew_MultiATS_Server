using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

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

    /// <summary>
    /// 条件1: DirectionSelfControlLeverがReversed && てこ位置と方向が不一致のDirectionRouteIdを取得
    /// </summary>
    /// <returns>DirectionRouteIdリスト</returns>
    public async Task<List<ulong>> GetIdsWhereLeverPositionMismatch()
    {
        return await context.DirectionRoutes
            .Join(context.DirectionSelfControlLevers.Include(dscl => dscl.DirectionSelfControlLeverState),
                dr => dr.DirectionSelfControlLeverId,
                dscl => dscl.Id,
                (dr, dscl) => new { dr, dscl })
            .Join(context.Levers.Include(l => l.LeverState),
                x => x.dr.LeverId,
                l => l.Id,
                (x, l) => new { x.dr, x.dscl, l })
            .Where(x =>
                x.dscl.DirectionSelfControlLeverState!.IsReversed == NR.Reversed &&
                (
                    (x.l.LeverState.IsReversed == LCR.Left && x.dr.DirectionRouteState!.isLr == LR.Right) ||
                    (x.l.LeverState.IsReversed == LCR.Right && x.dr.DirectionRouteState!.isLr == LR.Left)
                )
            )
            .Select(x => x.dr.Id)
            .ToListAsync();
    }

    /// <summary>
    /// 条件2: 総括制御元の進路のてこ反リレーが扛上していて、向きが違うDirectionRouteIdを取得
    /// </summary>
    /// <returns>DirectionRouteIdリスト</returns>
    public async Task<List<ulong>> GetIdsWhereThrowOutControlRaised()
    {
        return await context.DirectionRoutes
            .Join(context.ThrowOutControls,
                dr => dr.Id,
                toc => toc.TargetId,
                (dr, toc) => new { dr, toc })
            .Join(context.Routes.Include(r => r.RouteState),
                x => x.toc.SourceId,
                r => r.Id,
                (x, r) => new { x.dr, x.toc, r })
            .Where(x =>
                x.toc.ControlType == ThrowOutControlType.Direction &&
                x.r.RouteState!.IsLeverRelayRaised == RaiseDrop.Raise &&
                (
                    (x.toc.TargetLr == LR.Left && x.dr.DirectionRouteState.IsLRelayRaised == RaiseDrop.Drop) ||
                    (x.toc.TargetLr == LR.Right && x.dr.DirectionRouteState.IsRRelayRaised == RaiseDrop.Drop) ||
                    x.toc.TargetLr != x.dr.DirectionRouteState.isLr
                )
            )
            .Select(x => x.dr.Id)
            .ToListAsync();
    }

    public async Task<List<Models.DirectionRoute>> GetByIds(List<ulong> ids)
    {
        return await context.DirectionRoutes
            .Include(dr => dr.DirectionRouteState)
            .Where(dr => ids.Contains(dr.Id))
            .ToListAsync();
    }

}
