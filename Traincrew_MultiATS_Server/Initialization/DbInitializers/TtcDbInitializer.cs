using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLink;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes TTC (Train Traffic Control) window and window link entities in the database
/// </summary>
public class TtcDbInitializer(
    ILogger<TtcDbInitializer> logger,
    TtcWindowCsvLoader windowCsvLoader,
    TtcWindowLinkCsvLoader windowLinkCsvLoader,
    ITtcWindowRepository ttcWindowRepository,
    ITtcWindowLinkRepository ttcWindowLinkRepository,
    ITrackCircuitRepository trackCircuitRepository,
    IRouteRepository routeRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer
{
    /// <summary>
    ///     Initialize TTC windows from CSV file (TTC列番窓.csv)
    /// </summary>
    public async Task InitializeTtcWindowsAsync(IEnumerable<TtcWindowCsv> records, CancellationToken cancellationToken = default)
    {

        var existingWindows = (await ttcWindowRepository.GetAllWindowNamesAsync(cancellationToken)).ToHashSet();
        var trackCircuitIdByName = await trackCircuitRepository.GetAllIdForName(cancellationToken);

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

        await generalRepository.AddAll(ttcWindowsToAdd, cancellationToken);
        await generalRepository.AddAll(displayStationsToAdd, cancellationToken);
        await generalRepository.AddAll(windowTrackCircuitsToAdd, cancellationToken);
        logger.LogInformation("Initialized {Count} TTC windows", ttcWindowsToAdd.Count);
    }

    /// <summary>
    ///     Initialize TTC window links from CSV file (TTC列番窓リンク設定.csv)
    /// </summary>
    public async Task InitializeTtcWindowLinksAsync(IEnumerable<TtcWindowLinkCsv> records, CancellationToken cancellationToken = default)
    {
        var existingLinks = await ttcWindowLinkRepository.GetAllLinkPairsAsync(cancellationToken);
        var trackCircuitIdByName = await trackCircuitRepository.GetAllIdForName(cancellationToken);

        var ttcWindowLinksToAdd = new List<TtcWindowLink>();

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
        }

        await generalRepository.AddAll(ttcWindowLinksToAdd, cancellationToken);
        logger.LogInformation("Initialized {Count} TTC window links", ttcWindowLinksToAdd.Count);
    }

    /// <summary>
    ///     Initialize TTC window link route conditions from CSV file (TTC列番窓リンク設定.csv)
    /// </summary>
    public async Task InitializeTtcWindowLinkRouteConditionsAsync(IEnumerable<TtcWindowLinkCsv> records, CancellationToken cancellationToken = default)
    {
        var allLinks = await ttcWindowLinkRepository.GetAllTtcWindowLink();
        var linksByPair = allLinks.ToDictionary(
            link => (link.SourceTtcWindowName, link.TargetTtcWindowName),
            link => link
        );
        var routeIdByName = await routeRepository.GetAllIdForName(cancellationToken);

        var existingConditions = await ttcWindowLinkRepository.GetAllTtcWindowLinkRouteConditions();
        var existingPairs = existingConditions
            .Select(c => (c.TtcWindowLinkId, c.RouteId))
            .ToHashSet();

        var linkRouteConditionsToAdd = new List<TtcWindowLinkRouteCondition>();

        foreach (var record in records)
        {
            if (!linksByPair.TryGetValue((record.Source, record.Target), out var ttcWindowLink))
            {
                throw new InvalidOperationException($"TTC窓リンク (送信元: '{record.Source}', 送信先: '{record.Target}') が見つかりません。TTC窓リンク進路条件の初期化に失敗しました。");
            }

            foreach (var routeCondition in record.RouteConditions)
            {
                if (!routeIdByName.TryGetValue(routeCondition, out var routeId))
                {
                    throw new InvalidOperationException($"進路 '{routeCondition}' が見つかりません。TTC窓リンク進路条件 (送信元: '{record.Source}', 送信先: '{record.Target}') の初期化に失敗しました。");
                }

                if (existingPairs.Contains((ttcWindowLink.Id, routeId)))
                {
                    continue;
                }

                linkRouteConditionsToAdd.Add(new()
                {
                    RouteId = routeId,
                    TtcWindowLinkId = ttcWindowLink.Id
                });
            }
        }

        await generalRepository.AddAll(linkRouteConditionsToAdd, cancellationToken);
        logger.LogInformation("Initialized {Count} TTC window link route conditions", linkRouteConditionsToAdd.Count);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var windowRecords = await windowCsvLoader.LoadAsync(cancellationToken);
        var windowLinkRecords = await windowLinkCsvLoader.LoadAsync(cancellationToken);

        await InitializeTtcWindowsAsync(windowRecords, cancellationToken);
        await InitializeTtcWindowLinksAsync(windowLinkRecords, cancellationToken);
        await InitializeTtcWindowLinkRouteConditionsAsync(windowLinkRecords, cancellationToken);
    }
}