using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.IT.InterlockingLogic;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteLeverDestinationButton;
using Traincrew_MultiATS_Server.Repositories.SignalRoute;
using Traincrew_MultiATS_Server.Repositories.Station;
using Traincrew_MultiATS_Server.Repositories.ThrowOutControl;

namespace Traincrew_MultiATS_Server.IT.TestUtilities;

/// <summary>
/// 進路構成テストケースを動的に生成するヘルパークラス
/// </summary>
public class RouteTestCaseGenerator(
    IStationRepository stationRepository,
    IRouteLeverDestinationRepository routeLeverDestinationRepository,
    IRouteRepository routeRepository,
    ISignalRouteRepository signalRouteRepository,
    ILeverRepository leverRepository,
    IThrowOutControlRepository throwOutControlRepository)
{
    /// <summary>
    /// 指定された駅IDのテストケースを生成する
    /// </summary>
    /// <param name="stationIds">対象駅IDのリスト。nullの場合は全駅を対象とする</param>
    /// <returns>テストケースのリスト</returns>
    public async Task<List<RouteTestCase>> GenerateTestCasesAsync(string[]? stationIds = null)
    {
        // 1. 対象駅の取得
        var stationList = stationIds is { Length: > 0 }
            ? await stationRepository.GetStationByIds(stationIds)
            : await stationRepository.GetWhereIsStation();

        if (stationList.Count == 0)
        {
            return [];
        }

        var stations = stationList.ToDictionary(s => s.Id, s => s.Name);
        var targetStationIds = stations.Keys.ToList();

        // 2. 対象駅の進路を取得
        var routeList = await routeRepository.GetByStationIds(targetStationIds);
        var routes = routeList.ToDictionary(r => r.Id);
        var routeIds = routeList.Select(r => r.Id).ToList();

        // 3. 進路のRouteLeverDestinationButtonを取得
        var routeLeverButtons = await routeLeverDestinationRepository.GetByRouteIds(routeIds);

        // 4. LeverのIDを収集
        var leverIds = routeLeverButtons.Select(rlb => rlb.LeverId).Distinct().ToList();

        // 5. Leverを取得してDictionaryに変換
        var levers = (await leverRepository.GetByIdsWithState(leverIds))
            .ToDictionary(l => l.Id);

        // 6. 進路と信号機の関連を取得
        var signalRoutes = await signalRouteRepository.GetByRouteIds(routeIds);
        var signalRouteByRouteId = signalRoutes
            .GroupBy(sr => sr.RouteId)
            .ToDictionary(g => g.Key, g => g.First());

        // 7. てこなし総括(WithoutLever)の情報を取得
        var throwOutControls = await throwOutControlRepository
            .GetByControlTypes([ThrowOutControlType.WithoutLever]);

        // てこなし総括先の進路IDセット（これらはテストから除外する）
        var throwOutTargetRouteIds = throwOutControls
            .Select(toc => toc.TargetId)
            .ToHashSet();

        // てこなし総括元 -> 総括先のマッピング
        var throwOutSourceToTargets = throwOutControls
            .GroupBy(toc => toc.SourceId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(toc => toc.TargetId).ToList()
            );

        // 8. テストケース生成
        var testCases = routeLeverButtons
            .Select(rldb =>
            {
                var route = routes[rldb.RouteId];

                // てこなし総括先の進路はテストケースから除外
                if (throwOutTargetRouteIds.Contains(rldb.RouteId))
                {
                    return null;
                }

                // 辞書を使用してO(1)でルックアップ (O(N)になるのを避ける)
                if (!signalRouteByRouteId.TryGetValue(rldb.RouteId, out var signalRoute))
                {
                    // 信号機が関連付けられていない進路はスキップ
                    return null;
                }

                if (route.StationId == null)
                {
                    return null;
                }

                if (!stations.TryGetValue(route.StationId, out var stationName))
                {
                    return null;
                }

                if (!levers.TryGetValue(rldb.LeverId, out var lever))
                {
                    return null;
                }

                // てこなし総括元の場合、総括先の信号機名を取得
                List<string>? throwOutTargetSignals = null;
                if (throwOutSourceToTargets.TryGetValue(rldb.RouteId, out var targetRouteIds))
                {
                    throwOutTargetSignals = targetRouteIds
                        .Select(targetRouteId => signalRouteByRouteId.TryGetValue(targetRouteId, out var sr) ? sr.SignalName : null)
                        .Where(signalName => signalName != null)
                        .Cast<string>()
                        .ToList();
                }

                return new RouteTestCase
                {
                    StationId = route.StationId,
                    StationName = stationName,
                    RouteId = route.Id,
                    RouteName = route.Name,
                    LeverName = lever.Name,
                    LeverDirection = rldb.Direction == LR.Left ? LCR.Left : LCR.Right,
                    DestinationButtonName = rldb.DestinationButtonName,
                    SignalName = signalRoute.SignalName,
                    ThrowOutControlTargetSignals = throwOutTargetSignals
                };
            })
            .Where(tc => tc != null)
            .Cast<RouteTestCase>()
            .ToList();

        return testCases;
    }
}
