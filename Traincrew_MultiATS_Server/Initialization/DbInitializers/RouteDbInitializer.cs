using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteLockTrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes route lock track circuit relationships from CSV data
/// </summary>
public class RouteLockTrackCircuitDbInitializer(
    ILogger<RouteLockTrackCircuitDbInitializer> logger,
    RouteLockTrackCircuitCsvLoader csvLoader,
    IRouteRepository routeRepository,
    ITrackCircuitRepository trackCircuitRepository,
    IRouteLockTrackCircuitRepository routeLockTrackCircuitRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer
{
    /// <summary>
    ///     Initialize route lock track circuits from CSV file (進路.csv)
    /// </summary>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var records = await csvLoader.LoadAsync(cancellationToken);
        var routes = await routeRepository.GetIdsByName(cancellationToken);
        var trackCircuits = await trackCircuitRepository.GetAllIdsForName(cancellationToken);
        var routeLockTrackCircuits = (await routeLockTrackCircuitRepository
            .GetAll(cancellationToken))
            .Select(x => (x.RouteId, x.TrackCircuitId))
            .ToHashSet();

        var routeLockTrackCircuitList = new List<RouteLockTrackCircuit>();
        foreach (var record in records)
        {
            if (!routes.TryGetValue(record.RouteName, out var routeId))
            {
                throw new InvalidOperationException($"進路 '{record.RouteName}' が見つかりません。進路鎖錠軌道回路の初期化に失敗しました。");
            }

            foreach (var trackCircuitName in record.TrackCircuitNames)
            {
                if (!trackCircuits.TryGetValue(trackCircuitName, out var trackCircuitId))
                {
                    throw new InvalidOperationException($"軌道回路 '{trackCircuitName}' が見つかりません。進路 '{record.RouteName}' の進路鎖錠軌道回路の初期化に失敗しました。");
                }

                // 既に登録済みの場合、スキップ
                if (routeLockTrackCircuits.Contains((routeId, trackCircuitId)))
                {
                    continue;
                }

                routeLockTrackCircuitList.Add(new()
                {
                    RouteId = routeId,
                    TrackCircuitId = trackCircuitId
                });
            }
        }

        await generalRepository.AddAll(routeLockTrackCircuitList, cancellationToken);
        logger.LogInformation("Initialized {Count} route lock track circuits", routeLockTrackCircuitList.Count);
    }
}