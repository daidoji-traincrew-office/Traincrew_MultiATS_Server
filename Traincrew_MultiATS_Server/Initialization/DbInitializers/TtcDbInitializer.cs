using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;
using Traincrew_MultiATS_Server.Repositories.TtcWindowDisplayStation;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLink;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLinkRouteCondition;
using Traincrew_MultiATS_Server.Repositories.TtcWindowTrackCircuit;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes TTC (Train Traffic Control) window and window link entities in the database
/// </summary>
public class TtcDbInitializer(
    ILogger<TtcDbInitializer> logger,
    TtcWindowCsvLoader windowCsvLoader,
    TtcWindowLinkCsvLoader windowLinkCsvLoader,
    ITtcWindowRepository ttcWindowRepository,
    ITtcWindowDisplayStationRepository ttcWindowDisplayStationRepository,
    ITtcWindowTrackCircuitRepository ttcWindowTrackCircuitRepository,
    ITtcWindowLinkRepository ttcWindowLinkRepository,
    ITtcWindowLinkRouteConditionRepository ttcWindowLinkRouteConditionRepository,
    ITrackCircuitRepository trackCircuitRepository,
    IRouteRepository routeRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer(logger)
{
    /// <summary>
    ///     Initialize TTC windows from CSV file (TTC列番窓.csv)
    /// </summary>
    public async Task InitializeTtcWindowsAsync(CancellationToken cancellationToken = default)
    {
        var records = await windowCsvLoader.LoadAsync(cancellationToken);

        var existingWindows = await ttcWindowRepository.GetAllWindowNamesAsync(cancellationToken);
        var trackCircuitIdByName = await trackCircuitRepository.GetIdsByName(cancellationToken);

        var ttcWindowsToAdd = new List<TtcWindow>();
        var displayStationsToAdd = new List<TtcWindowDisplayStation>();
        var windowTrackCircuitsToAdd = new List<TtcWindowTrackCircuit>();

        foreach (var record in records)
        {
            if (existingWindows.Contains(record.Name))
            {
                continue;
            }

            ttcWindowsToAdd.Add(new()
            {
                Name = record.Name,
                StationId = record.StationId,
                Type = record.Type,
                TtcWindowState = new()
                {
                    TrainNumber = ""
                }
            });

            foreach (var displayStation in record.DisplayStations)
            {
                displayStationsToAdd.Add(new()
                {
                    TtcWindowName = record.Name,
                    StationId = displayStation
                });
            }

            foreach (var trackCircuit in record.TrackCircuits)
            {
                windowTrackCircuitsToAdd.Add(new()
                {
                    TtcWindowName = record.Name,
                    TrackCircuitId = trackCircuitIdByName[trackCircuit]
                });
            }
        }

        await generalRepository.AddAll(ttcWindowsToAdd);
        await generalRepository.AddAll(displayStationsToAdd);
        await generalRepository.AddAll(windowTrackCircuitsToAdd);
        _logger.LogInformation("Initialized {Count} TTC windows", ttcWindowsToAdd.Count);
    }

    /// <summary>
    ///     Initialize TTC window links from CSV file (TTC列番窓リンク設定.csv)
    /// </summary>
    public async Task InitializeTtcWindowLinksAsync(CancellationToken cancellationToken = default)
    {
        var records = await windowLinkCsvLoader.LoadAsync(cancellationToken);

        var existingLinks = await ttcWindowLinkRepository.GetAllLinkPairsAsync(cancellationToken);
        var trackCircuitIdByName = await trackCircuitRepository.GetIdsByName(cancellationToken);
        var routeIdByName = await routeRepository.GetIdsByName(cancellationToken);

        var ttcWindowLinksToAdd = new List<TtcWindowLink>();
        var linkRouteConditionsToAdd = new List<TtcWindowLinkRouteCondition>();

        foreach (var record in records)
        {
            if (existingLinks.Contains((record.Source, record.Target)))
            {
                continue;
            }

            var ttcWindowLink = new TtcWindowLink
            {
                SourceTtcWindowName = record.Source,
                TargetTtcWindowName = record.Target,
                Type = record.Type,
                IsEmptySending = record.IsEmptySending,
                TrackCircuitCondition = record.TrackCircuitCondition != null
                    ? trackCircuitIdByName[record.TrackCircuitCondition]
                    : null
            };
            ttcWindowLinksToAdd.Add(ttcWindowLink);

            foreach (var routeCondition in record.RouteConditions)
            {
                if (!routeIdByName.TryGetValue(routeCondition, out var routeId))
                {
                    continue;
                }

                linkRouteConditionsToAdd.Add(new()
                {
                    RouteId = routeId,
                    TtcWindowLink = ttcWindowLink
                });
            }
        }

        await generalRepository.AddAll(ttcWindowLinksToAdd);
        await generalRepository.AddAll(linkRouteConditionsToAdd);
        _logger.LogInformation("Initialized {Count} TTC window links", ttcWindowLinksToAdd.Count);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await InitializeTtcWindowsAsync(cancellationToken);
        await InitializeTtcWindowLinksAsync(cancellationToken);
    }
}