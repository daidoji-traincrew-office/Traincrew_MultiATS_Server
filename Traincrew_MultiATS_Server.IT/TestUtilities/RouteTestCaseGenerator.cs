using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.IT.InterlockingLogic;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.IT.TestUtilities;

/// <summary>
/// 進路構成テストケースを動的に生成するヘルパークラス
/// </summary>
public class RouteTestCaseGenerator(ApplicationDbContext context)
{
    /// <summary>
    /// 指定された駅IDのテストケースを生成する
    /// </summary>
    /// <param name="stationIds">対象駅IDのリスト。nullの場合は全駅を対象とする</param>
    /// <returns>テストケースのリスト</returns>
    public async Task<List<RouteTestCase>> GenerateTestCasesAsync(string[]? stationIds = null)
    {
        // 1. 対象駅の取得
        IQueryable<Station> stationQuery = context.Stations;
        if (stationIds is { Length: > 0 })
        {
            stationQuery = stationQuery.Where(s => stationIds.Contains(s.Id));
        }

        var stations = await stationQuery.ToDictionaryAsync(s => s.Id, s => s.Name);

        if (stations.Count == 0)
        {
            return [];
        }

        var targetStationIds = stations.Keys.ToList();

        // 2. 対象駅の進路とてこ・着点の関連を取得
        var routeLeverButtons = await context.RouteLeverDestinationButtons
            .Include(rldb => rldb.Lever)
            .Include(rldb => rldb.DestinationButton)
            .Where(rldb => targetStationIds.Contains(rldb.Lever.StationId!))
            .ToListAsync();

        // 3. 進路IDのリストを取得
        var routeIds = routeLeverButtons.Select(rldb => rldb.RouteId).Distinct().ToList();

        // 4. 進路情報を取得
        var routes = await context.Routes
            .Where(r => routeIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id);

        // 5. 進路と信号機の関連を取得
        var signalRoutes = await context.SignalRoutes
            .Where(sr => routeIds.Contains(sr.RouteId))
            .ToListAsync();

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
