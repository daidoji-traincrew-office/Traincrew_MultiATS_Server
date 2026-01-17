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
            // Todo: 該当進路が登録されていない場合エラー
            if (!routes.TryGetValue(record.RouteName, out var routeId))
            {
                continue;
            }

            foreach (var trackCircuitName in record.TrackCircuitNames)
            {
                // Todo: 該当軌道回路が登録されていない場合エラー
                if (!trackCircuits.TryGetValue(trackCircuitName, out var trackCircuitId))
                {
                    continue;
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