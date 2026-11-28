using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TtcPath;
using Traincrew_MultiATS_Server.Repositories.TtcPathApproachTrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TtcPathPredecessor;
using Traincrew_MultiATS_Server.Repositories.TtcPathRoute;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;
using Traincrew_MultiATS_Server.Repositories.TtcWindowLink;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes TTC (Train Traffic Control) window and window link entities in the database
/// </summary>
public class TtcDbInitializer(
    ILogger<TtcDbInitializer> logger,
    TtcPathCsvLoader pathCsvLoader,
    TtcWindowCsvLoader windowCsvLoader,
    TtcWindowLinkCsvLoader windowLinkCsvLoader,
    ITtcPathRepository ttcPathRepository,
    ITtcPathRouteRepository ttcPathRouteRepository,
    ITtcPathApproachTrackCircuitRepository ttcPathApproachTrackCircuitRepository,
    ITtcPathPredecessorRepository ttcPathPredecessorRepository,
    ITtcWindowRepository ttcWindowRepository,
    ITtcWindowLinkRepository ttcWindowLinkRepository,
    ITrackCircuitRepository trackCircuitRepository,
    IRouteRepository routeRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer
{
    /// <summary>
    ///     Initialize TTC paths from CSV file (TTC進路.csv)
    /// </summary>
    public async Task InitializeTtcPathAsync(IEnumerable<TtcPathCsv> records,
        CancellationToken cancellationToken = default)
    {
        var existingPaths = await ttcPathRepository.GetAll(cancellationToken);
        var existingPathByName = existingPaths.ToDictionary(x => x.Name);

        var pathToAdd = new List<TtcPath>();
        var pathToUpdate = new List<TtcPath>();

        foreach (var record in records)
        {
            if (existingPathByName.TryGetValue(record.Name, out var existingPath))
            {
                if (
                    existingPath.StationId == record.Station
                    && existingPath.SourceTrack == record.SourceTrack
                    && existingPath.TargetTrack == record.TargetTrack
                    && existingPath.Type == record.Type
                    && existingPath.DefineOrderTrackCircuitCount == record.DefineOrderTrackCircuitCount
                )
                {
                    continue;
                }

                existingPath.StationId = record.Station;
                existingPath.SourceTrack = record.SourceTrack;
                existingPath.TargetTrack = record.TargetTrack;
                existingPath.Type = record.Type;
                existingPath.DefineOrderTrackCircuitCount = record.DefineOrderTrackCircuitCount;
                pathToUpdate.Add(existingPath);
            }
            else
            {
                pathToAdd.Add(new TtcPath
                {
                    Name = record.Name,
                    StationId = record.Station,
                    SourceTrack = record.SourceTrack,
                    TargetTrack = record.TargetTrack,
                    Type = record.Type,
                    DefineOrderTrackCircuitCount = record.DefineOrderTrackCircuitCount
                });
            }
        }

        if (pathToAdd.Count > 0)
        {
            await generalRepository.AddAll(pathToAdd, cancellationToken);
        }

        if (pathToUpdate.Count > 0)
        {
            await generalRepository.SaveAll(pathToUpdate, cancellationToken);
        }
    }


    /// <summary>
    ///     Initialize TTC path routes (対応進路) from CSV file (TTC進路.csv)
    /// </summary>
    public async Task InitializeTtcPathRoutesAsync(IEnumerable<TtcPathCsv> records,
        CancellationToken cancellationToken = default)
    {
        var routeIdByName = await routeRepository.GetAllIdForName(cancellationToken);
        var existing = await ttcPathRouteRepository.GetAll(cancellationToken);
        var count = await SyncTtcPathChildrenAsync(
            records,
            record => record.TargetRoutes,
            routeIdByName,
            "進路",
            "TTC進路の対応進路",
            existing,
            x => x.TtcPathId,
            x => x.Index,
            x => x.RouteId,
            (pathId, index, refId) => new Models.TtcPathRoute { TtcPathId = pathId, Index = index, RouteId = refId },
            cancellationToken);
        logger.LogInformation("Initialized {Count} TTC path routes", count);
    }

    /// <summary>
    ///     Initialize TTC path approach track circuits (接近判定) from CSV file (TTC進路.csv)
    /// </summary>
    public async Task InitializeTtcPathApproachTrackCircuitsAsync(IEnumerable<TtcPathCsv> records,
        CancellationToken cancellationToken = default)
    {
        var trackCircuitIdByName = await trackCircuitRepository.GetAllIdForName(cancellationToken);
        var existing = await ttcPathApproachTrackCircuitRepository.GetAll(cancellationToken);
        var count = await SyncTtcPathChildrenAsync(
            records,
            record => record.ApproachTrackCircuits,
            trackCircuitIdByName,
            "軌道回路",
            "TTC進路の接近判定",
            existing,
            x => x.TtcPathId,
            x => x.Index,
            x => x.TrackCircuitId,
            (pathId, index, refId) => new TtcPathApproachTrackCircuit
                { TtcPathId = pathId, Index = index, TrackCircuitId = refId },
            cancellationToken);
        logger.LogInformation("Initialized {Count} TTC path approach track circuits", count);
    }

    /// <summary>
    ///     Initialize TTC path predecessors (近接前対応) from CSV file (TTC進路.csv)
    /// </summary>
    public async Task InitializeTtcPathPredecessorsAsync(IEnumerable<TtcPathCsv> records,
        CancellationToken cancellationToken = default)
    {
        var pathIdByName = (await ttcPathRepository.GetAll(cancellationToken))
            .ToDictionary(x => x.Name, x => x.Id);
        var existing = await ttcPathPredecessorRepository.GetAll(cancellationToken);
        var count = await SyncTtcPathChildrenAsync(
            records,
            record => record.Predecessor,
            pathIdByName,
            "TTC進路",
            "TTC進路の近接前対応",
            existing,
            x => x.SourceTtcPathId,
            x => x.Index,
            x => x.TargetTtcPathId,
            (pathId, index, refId) => new TtcPathPredecessor
                { SourceTtcPathId = pathId, Index = index, TargetTtcPathId = refId },
            cancellationToken);
        logger.LogInformation("Initialized {Count} TTC path predecessors", count);
    }

    /// <summary>
    ///     TTC進路の子テーブルを、パス単位で CSV の内容に揃える。
    ///     (index, 参照先Id) の並びが一致するパスは何もせず、違うパスは既存行を全削除してから作り直す。
    /// </summary>
    /// <returns>追加した行数</returns>
    private async Task<int> SyncTtcPathChildrenAsync<TEntity>(
        IEnumerable<TtcPathCsv> records,
        Func<TtcPathCsv, List<string>> selectNames,
        IReadOnlyDictionary<string, ulong> refIdByName,
        string refLabel,
        string tableLabel,
        List<TEntity> existing,
        Func<TEntity, ulong> pathIdOf,
        Func<TEntity, int> indexOf,
        Func<TEntity, ulong> refIdOf,
        Func<ulong, int, ulong, TEntity> create,
        CancellationToken cancellationToken)
    {
        var pathIdByName = (await ttcPathRepository.GetAll(cancellationToken))
            .ToDictionary(x => x.Name, x => x.Id);
        var existingByPathId = existing
            .GroupBy(pathIdOf)
            .ToDictionary(g => g.Key, g => g.OrderBy(indexOf).ToList());

        var toDelete = new List<TEntity>();
        var toAdd = new List<TEntity>();

        foreach (var record in records)
        {
            if (!pathIdByName.TryGetValue(record.Name, out var pathId))
            {
                throw new InvalidOperationException(
                    $"TTC進路 '{record.Name}' が見つかりません。{tableLabel}の初期化に失敗しました。");
            }

            var desired = new List<(int Index, ulong RefId)>();
            foreach (var name in selectNames(record))
            {
                if (!refIdByName.TryGetValue(name, out var refId))
                {
                    throw new InvalidOperationException(
                        $"{refLabel} '{name}' が見つかりません。{tableLabel} (TTC進路: '{record.Name}') の初期化に失敗しました。");
                }

                desired.Add((desired.Count, refId));
            }

            existingByPathId.TryGetValue(pathId, out var current);
            current ??= [];
            var currentPairs = current.Select(x => (indexOf(x), refIdOf(x)));
            if (currentPairs.SequenceEqual(desired))
            {
                continue;
            }

            toDelete.AddRange(current);
            toAdd.AddRange(desired.Select(x => create(pathId, x.Index, x.RefId)));
        }

        // UNIQUE(ttc_path_id, index) 衝突を避けるため、削除と追加は別々の SaveChanges で行う
        if (toDelete.Count > 0)
        {
            await generalRepository.DeleteAll(toDelete, cancellationToken);
        }

        if (toAdd.Count > 0)
        {
            await generalRepository.AddAll(toAdd, cancellationToken);
        }

        return toAdd.Count;
    }

    /// <summary>
    ///     Initialize TTC windows from CSV file (TTC列番窓.csv)
    /// </summary>
    public async Task InitializeTtcWindowsAsync(IEnumerable<TtcWindowCsv> records,
        CancellationToken cancellationToken = default)
    {
        var existingWindowByName = (await ttcWindowRepository.GetAll(cancellationToken))
            .ToDictionary(w => w.Name);
        var trackCircuitIdByName = await trackCircuitRepository.GetAllIdForName(cancellationToken);

        // 同じ駅で同じ番線の窓が複数あると、部分一意インデックスに違反する
        var duplicated = records
            .Where(r => r.TrackNumber != "")
            .GroupBy(r => (r.StationId, r.TrackNumber))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicated != null)
        {
            throw new InvalidOperationException(
                $"TTC列番窓の番線が重複しています。駅 '{duplicated.Key.StationId}' の番線 '{duplicated.Key.TrackNumber}': " +
                $"{string.Join(", ", duplicated.Select(r => r.Name))}");
        }

        var ttcWindowsToAdd = new List<TtcWindow>();
        var ttcWindowsToUpdate = new List<TtcWindow>();
        var desiredByWindowName = new Dictionary<string, (string TrackNumber, TtcWindowType Type)>();
        var displayStationsToAdd = new List<TtcWindowDisplayStation>();
        var windowTrackCircuitsToAdd = new List<TtcWindowTrackCircuit>();

        foreach (var record in records)
        {
            if (existingWindowByName.TryGetValue(record.Name, out var existingWindow))
            {
                // 既存の窓は、番線と種類をCSVに揃える(追加時にしか入らないと、既存DBの窓に反映されない)
                if (existingWindow.TrackNumber != record.TrackNumber || existingWindow.Type != record.Type)
                {
                    ttcWindowsToUpdate.Add(existingWindow);
                    desiredByWindowName[existingWindow.Name] = (record.TrackNumber, record.Type);
                }

                continue;
            }

            ttcWindowsToAdd.Add(new()
            {
                Name = record.Name,
                StationId = record.StationId,
                TrackNumber = record.TrackNumber,
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

        // CSVに無い既存窓は、行は残して番線だけ空文字にする(番線が空でないものだけ)
        var csvWindowNames = records.Select(r => r.Name).ToHashSet();
        var ttcWindowsToClear = existingWindowByName.Values
            .Where(w => !csvWindowNames.Contains(w.Name) && w.TrackNumber != "")
            .ToList();

        // 番線の入れ替え(A=1,B=2 → A=2,B=1)や、消えた窓の番線の付け替えで部分一意インデックスに
        // 違反しないよう、いったん空文字にして保存してから、目的の番線で保存する
        var ttcWindowsToEmpty = ttcWindowsToClear.Concat(ttcWindowsToUpdate).ToList();
        if (ttcWindowsToEmpty.Count > 0)
        {
            foreach (var window in ttcWindowsToEmpty)
            {
                window.TrackNumber = "";
            }

            await generalRepository.SaveAll(ttcWindowsToEmpty, cancellationToken);
        }

        if (ttcWindowsToUpdate.Count > 0)
        {
            foreach (var window in ttcWindowsToUpdate)
            {
                var desired = desiredByWindowName[window.Name];
                window.TrackNumber = desired.TrackNumber;
                window.Type = desired.Type;
            }

            await generalRepository.SaveAll(ttcWindowsToUpdate, cancellationToken);
        }

        await generalRepository.AddAll(ttcWindowsToAdd, cancellationToken);
        await generalRepository.AddAll(displayStationsToAdd, cancellationToken);
        await generalRepository.AddAll(windowTrackCircuitsToAdd, cancellationToken);
        logger.LogInformation(
            "Initialized {Count} TTC windows, updated {UpdatedCount} TTC window track numbers or types, cleared {ClearedCount} TTC window track numbers",
            ttcWindowsToAdd.Count, ttcWindowsToUpdate.Count, ttcWindowsToClear.Count);
    }

    /// <summary>
    ///     Initialize TTC window links from CSV file (TTC列番窓リンク設定.csv)
    /// </summary>
    public async Task InitializeTtcWindowLinksAsync(IEnumerable<TtcWindowLinkCsv> records,
        CancellationToken cancellationToken = default)
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
    public async Task InitializeTtcWindowLinkRouteConditionsAsync(IEnumerable<TtcWindowLinkCsv> records,
        CancellationToken cancellationToken = default)
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
                throw new InvalidOperationException(
                    $"TTC窓リンク (送信元: '{record.Source}', 送信先: '{record.Target}') が見つかりません。TTC窓リンク進路条件の初期化に失敗しました。");
            }

            foreach (var routeCondition in record.RouteConditions)
            {
                if (!routeIdByName.TryGetValue(routeCondition, out var routeId))
                {
                    throw new InvalidOperationException(
                        $"進路 '{routeCondition}' が見つかりません。TTC窓リンク進路条件 (送信元: '{record.Source}', 送信先: '{record.Target}') の初期化に失敗しました。");
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
        var pathRecords = await pathCsvLoader.LoadAsync(cancellationToken);
        var windowRecords = await windowCsvLoader.LoadAsync(cancellationToken);
        var windowLinkRecords = await windowLinkCsvLoader.LoadAsync(cancellationToken);

        await InitializeTtcPathAsync(pathRecords, cancellationToken);
        await InitializeTtcPathRoutesAsync(pathRecords, cancellationToken);
        await InitializeTtcPathApproachTrackCircuitsAsync(pathRecords, cancellationToken);
        await InitializeTtcPathPredecessorsAsync(pathRecords, cancellationToken);
        await InitializeTtcWindowsAsync(windowRecords, cancellationToken);
        await InitializeTtcWindowLinksAsync(windowLinkRecords, cancellationToken);
        await InitializeTtcWindowLinkRouteConditionsAsync(windowLinkRecords, cancellationToken);
    }
}