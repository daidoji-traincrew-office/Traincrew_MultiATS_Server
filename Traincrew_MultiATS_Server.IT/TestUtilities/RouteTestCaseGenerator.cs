using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.IT.InterlockingLogic;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Lever;
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
    IRouteLeverDestinationRepository routeLeverDestinationRepository,
    IRouteRepository routeRepository,
    ISignalRouteRepository signalRouteRepository,
    ILeverRepository leverRepository)
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

        // 7. テストケース生成
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

                if (!levers.TryGetValue(rldb.LeverId, out var lever))
                {
                    return null;
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
                    SignalName = signalRoute.SignalName
                };
            })
            .Where(tc => tc != null)
            .Cast<RouteTestCase>()
            .ToList();

        return testCases;
    }
}
