using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.IT.InterlockingLogic;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteLeverDestinationButton;
using Traincrew_MultiATS_Server.Repositories.SignalRoute;
using Traincrew_MultiATS_Server.Repositories.Station;

namespace Traincrew_MultiATS_Server.IT.TestUtilities;

/// <summary>
/// 進路構成テストケースを動的に生成するヘルパークラス
/// </summary>
public class RouteTestCaseGenerator(
    IStationRepository stationRepository,
    IRouteLeverDestinationButtonRepository routeLeverDestinationButtonRepository,
    IRouteRepository routeRepository,
    ISignalRouteRepository signalRouteRepository)
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

        // 2. 対象駅の進路とてこ・着点の関連を取得
        var routeLeverButtons = await routeLeverDestinationButtonRepository.GetByStationIdsWithNavigations(targetStationIds);

        // 3. 進路IDのリストを取得
        var routeIds = routeLeverButtons.Select(rldb => rldb.RouteId).Distinct().ToList();

        // 4. 進路情報を取得
        var routeList = await routeRepository.GetByIdsWithState(routeIds);
        var routes = routeList.ToDictionary(r => r.Id);

        // 5. 進路と信号機の関連を取得
        var signalRoutes = await signalRouteRepository.GetByRouteIds(routeIds);

        // 6. テストケース生成
        var testCases = routeLeverButtons
            .Select(rldb =>
            {
                var route = routes[rldb.RouteId];
                var signalRoute = signalRoutes.FirstOrDefault(sr => sr.RouteId == rldb.RouteId);

                // 信号機が関連付けられていない進路はスキップ
                if (signalRoute == null)
                {
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

                return new RouteTestCase
                {
                    StationId = route.StationId,
                    StationName = stationName,
                    RouteId = route.Id,
                    RouteName = route.Name,
                    LeverName = rldb.Lever.Name,
                    LeverDirection = rldb.Direction == LR.Left ? LCR.Left : LCR.Right,
                    DestinationButtonName = rldb.DestinationButtonName,
                    SignalName = signalRoute.SignalName
                };
            })
            .Where(tc => tc != null)
            .Cast<RouteTestCase>()
            .ToList();

        return testCases;
    }
}
