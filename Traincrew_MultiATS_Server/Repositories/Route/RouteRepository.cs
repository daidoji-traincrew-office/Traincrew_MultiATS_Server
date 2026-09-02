using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Route;

public class RouteRepository(ApplicationDbContext context) : IRouteRepository
{
    public async Task<List<Models.Route>> GetByIdsWithState(List<ulong> ids)
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();
    }

    public async Task<List<Models.Route>> GetByTcNameWithState(string tcName)
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Where(r => r.TcName == tcName)
            .ToListAsync();
    }

    public async Task<List<Models.Route>> GetByStationIds(List<string> stationIds)
    {
        return await context.Routes
            .Where(r => r.StationId != null && stationIds.Contains(r.StationId))
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsToOpenWithThrowOutControl()
    {
        return await context.ThrowOutControls
            .Join(context.RouteLeverDestinationButtons,
                toc => toc.SourceId,
                rldb => rldb.RouteId,
                (toc, rldb) => new { toc, source = rldb }
            )
            .Join(context.RouteLeverDestinationButtons,
                combined => combined.toc.TargetId,
                rldb => rldb.RouteId,
                (combined, rldb) => new
                {
                    combined.toc,
                    combined.source,
                    target = rldb
                }
            )
            .Where(combined =>
                (
                    combined.source.Lever.LeverState.IsReversed == LCR.Left && combined.source.Direction == LR.Left
                    || combined.source.Lever.LeverState.IsReversed == LCR.Right && combined.source.Direction == LR.Right
                )
                && (
                    combined.target.DestinationButtonName == null
                    || combined.target.DestinationButton.DestinationButtonState.IsRaised == RaiseDrop.Raise
                )
            )
            .SelectMany(combined => new[] { combined.toc.SourceId, combined.toc.TargetId }.AsEnumerable())
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsWhereRouteSecured()
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Where(r => r.RouteState.IsRouteSecured == RaiseDrop.Raise)
            .Select(r => r.Id)
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsForAll()
    {
        return await context.Routes
            .Include(r => r.RouteState)
            .Select(r => r.Id)
            .ToListAsync();
    }



    /// <summary>
    /// 進路名から進路IDへのマッピングを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>進路名をキー、進路IDを値とする辞書</returns>
    public async Task<Dictionary<string, ulong>> GetAllIdForName(CancellationToken cancellationToken = default)
    {
        return await context.Routes
            .Select(r => new { r.Name, r.Id })
            .ToDictionaryAsync(r => r.Name, r => r.Id, cancellationToken);
    }

    /// <summary>
    /// 進路名から進路エンティティへのマッピングを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>進路名をキー、進路エンティティを値とする辞書</returns>
    public async Task<Dictionary<string, Models.Route>> GetByNames(CancellationToken cancellationToken = default)
    {
        return await context.Routes
            .ToDictionaryAsync(r => r.Name, cancellationToken);
    }

    /// <summary>
    /// 指定したIDのRouteStateのIsSignalControlRaisedのみを更新する
    /// (route_stateは2プロセスが別々の列を書くため、列限定更新にすること)
    /// </summary>
    /// <param name="ids">進路のIDリスト</param>
    /// <param name="isSignalControlRaised">信号制御リレー状態</param>
    public async Task SetIsSignalControlRaisedByIds(List<ulong> ids, RaiseDrop isSignalControlRaised)
    {
        await context.RouteStates
            .Where(routeState => ids.Contains(routeState.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(routeState => routeState.IsSignalControlRaised, isSignalControlRaised));
    }

    /// <summary>
    /// 指定したIDのRouteStateのIsRouteSecuredのみを更新する
    /// (route_stateは2プロセスが別々の列を書くため、列限定更新にすること)
    /// </summary>
    /// <param name="ids">進路のIDリスト</param>
    /// <param name="isRouteSecured">進路確保中か</param>
    public async Task SetIsRouteSecuredByIds(List<ulong> ids, RaiseDrop isRouteSecured)
    {
        await context.RouteStates
            .Where(routeState => ids.Contains(routeState.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(routeState => routeState.IsRouteSecured, isRouteSecured));
    }

    /// <summary>
    /// 指定したIDのRouteStateのIsCtcControlledのみを更新する
    /// (route_stateは2プロセスが別々の列を書くため、列限定更新にすること)
    /// </summary>
    /// <param name="ids">進路のIDリスト</param>
    /// <param name="isCtcControlled">CTC制御中か</param>
    public async Task SetIsCtcControlledByIds(List<ulong> ids, RaiseDrop isCtcControlled)
    {
        await context.RouteStates
            .Where(routeState => ids.Contains(routeState.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(routeState => routeState.IsCtcControlled, isCtcControlled));
    }
}
