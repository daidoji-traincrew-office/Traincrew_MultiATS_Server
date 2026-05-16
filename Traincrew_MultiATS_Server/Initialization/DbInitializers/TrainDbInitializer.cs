using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DiagramTrain;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.TrainType;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes train type and train diagram entities in the database
/// </summary>
public class TrainDbInitializer(
    ILogger<TrainDbInitializer> logger,
    ITrainTypeRepository trainTypeRepository,
    IDiagramTrainRepository diagramTrainRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer
{
    /// <summary>
    ///     Initialize train types from CSV data
    /// </summary>
    public async Task InitializeTrainTypesAsync(List<TrainTypeCsv> trainTypeList,
        CancellationToken cancellationToken = default)
    {
        var existingIds = await trainTypeRepository.GetIdsForAll(cancellationToken);

        List<TrainType> trainTypes = [];
        foreach (var record in trainTypeList)
        {
            if (existingIds.Contains(record.Id))
            {
                continue;
            }

            trainTypes.Add(new()
            {
                Id = record.Id,
                Name = record.Name
            });
        }

        await generalRepository.AddAll(trainTypes, cancellationToken);
        logger.LogInformation("Initialized {Count} train types", trainTypes.Count);
    }

    /// <summary>
    ///     Initialize train diagrams from CSV data
    /// </summary>
    public async Task InitializeTrainDiagramsAsync(List<DiagramTrainCsv> trainDiagramList,
        CancellationToken cancellationToken = default)
    {
        var existingNumbers = await diagramTrainRepository.GetTrainNumbersForAll(cancellationToken);

        List<DiagramTrain> diagramTrains = [];
        foreach (var record in trainDiagramList)
        {
            if (existingNumbers.Contains(record.TrainNumber))
            {
                continue;
            }

            diagramTrains.Add(new()
            {
                TrainNumber = record.TrainNumber,
                TrainTypeId = record.TypeId,
                FromStationId = record.FromStationId,
                ToStationId = record.ToStationId,
                DiaId = record.DiaId
            });
        }

        await generalRepository.AddAll(diagramTrains, cancellationToken);
        logger.LogInformation("Initialized {Count} train diagrams", diagramTrains.Count);
    }

    /// <summary>
    ///     Initialize train diagrams and timetables from TTC_Data
    /// </summary>
    public async Task InitializeFromTtcDataAsync(TTC_Data ttcData, ulong diaId = 1, CancellationToken cancellationToken = default)
    {
        // 既存のTrainDiagramをdiaIdで取得
        var existingDiagrams = await diagramTrainRepository.GetForTrainNumberByDiaId(diaId, cancellationToken);
        var trainTypeIdByName = await trainTypeRepository.GetAllIdForName(cancellationToken);

        // diaIdに紐づくTimetablesを全削除 (ExecuteDelete)
        await diagramTrainRepository.DeleteTimetablesByDiaId(diaId, cancellationToken);
        logger.LogInformation("Deleted existing timetables for diaId {DiaId}", diaId);

        // 同じData内に重複する列番を事前に検出し、すべてスキップ対象とする
        var duplicateTrainNumbers = ttcData.TrainList
            .Where(t => !string.IsNullOrWhiteSpace(t.trainNumber))
            .GroupBy(t => t.trainNumber)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();
        foreach (var duplicateTrainNumber in duplicateTrainNumbers)
        {
            logger.LogWarning("列車番号 {TrainNumber} が重複しています。すべてのデータをスキップします。", duplicateTrainNumber);
        }

        List<DiagramTrain> trainDiagramsToAdd = [];
        List<DiagramTrain> trainDiagramsToUpdate = [];
        Dictionary<string, List<DiagramTrainTimetable>> trainTimetablesByTrainNumber = [];

        foreach (var ttcTrain in ttcData.TrainList)
        {
            if (string.IsNullOrWhiteSpace(ttcTrain.trainNumber))
            {
                logger.LogWarning("列車番号が空です。{operationNumber} スキップします。", ttcTrain.operationNumber);
                continue;
            }

            if (duplicateTrainNumbers.Contains(ttcTrain.trainNumber))
            {
                continue;
            }

            // Get train type ID from train class name, default to 1 if not found
            var trainTypeId = ResolveTrainTypeId(trainTypeIdByName, ttcTrain.trainClass, ttcTrain.trainName);

            var newDiagram = new DiagramTrain
            {
                TrainNumber = ttcTrain.trainNumber,
                TrainTypeId = trainTypeId,
                FromStationId = ttcTrain.originStationID,
                ToStationId = ttcTrain.destinationStationID,
                DiaId = diaId
            };

            // 既存のTrainDiagramと差分チェック
            if (existingDiagrams.TryGetValue(ttcTrain.trainNumber, out var existingDiagram))
            {
                // 差分がある場合のみ更新
                if (existingDiagram.TrainTypeId != newDiagram.TrainTypeId ||
                    existingDiagram.FromStationId != newDiagram.FromStationId ||
                    existingDiagram.ToStationId != newDiagram.ToStationId
                    )
                {
                    existingDiagram.TrainTypeId = newDiagram.TrainTypeId;
                    existingDiagram.FromStationId = newDiagram.FromStationId;
                    existingDiagram.ToStationId = newDiagram.ToStationId;
                    trainDiagramsToUpdate.Add(existingDiagram);
                }
            }
            else
            {
                // 新規追加
                trainDiagramsToAdd.Add(newDiagram);
            }

            // Create DiagramTrainTimetable for each TTC_StationData (IDは後で設定)
            var timetables = ttcTrain.staList.Select((stationData, i) => new DiagramTrainTimetable()
            {
                Index = i + 1,
                StationId = stationData.stationID,
                TrackNumber = stationData.stopPosName ?? "",
                ArrivalTime = ConvertToTimeSpan(stationData.arrivalTime),
                DepartureTime = ConvertToTimeSpan(stationData.departureTime)
            }).ToList();

            trainTimetablesByTrainNumber[ttcTrain.trainNumber] = timetables;
        }

        // TrainDiagramの追加・更新
        if (trainDiagramsToAdd.Count > 0)
        {
            await generalRepository.AddAll(trainDiagramsToAdd, cancellationToken);
            logger.LogInformation("Added {Count} new train diagrams", trainDiagramsToAdd.Count);
        }

        if (trainDiagramsToUpdate.Count > 0)
        {
            await generalRepository.SaveAll(trainDiagramsToUpdate, cancellationToken);
            logger.LogInformation("Updated {Count} train diagrams", trainDiagramsToUpdate.Count);
        }

        // 全TrainDiagram(追加+更新)のIDを取得して、TrainDiagramIdを設定
        var allDiagrams = await diagramTrainRepository.GetForTrainNumberByDiaId(diaId, cancellationToken);
        List<DiagramTrainTimetable> trainTimetables = [];

        foreach (var (trainNumber, timetables) in trainTimetablesByTrainNumber)
        {
            if (!allDiagrams.TryGetValue(trainNumber, out var diagram))
            {
                continue;
            }
            foreach (var timetable in timetables)
            {
                timetable.TrainDiagramId = diagram.Id;
                trainTimetables.Add(timetable);
            }
        }

        // Timetablesを再追加
        if (trainTimetables.Count > 0)
        {
            await generalRepository.AddAll(trainTimetables, cancellationToken);
            logger.LogInformation("Added {Count} timetables", trainTimetables.Count);
        }

        logger.LogInformation("Completed initialization from TTC_Data: {AddCount} added, {UpdateCount} updated train diagrams, {TimetableCount} timetables, {DuplicateCount} duplicate train numbers skipped",
            trainDiagramsToAdd.Count, trainDiagramsToUpdate.Count, trainTimetables.Count, duplicateTrainNumbers.Count);
    }

    private long ResolveTrainTypeId(Dictionary<string, long> trainTypeIdByName, string trainClass, string? trainName)
    {
        if (trainTypeIdByName.TryGetValue(trainClass, out var id))
        {
            return id;
        }

        if (trainClass == "特急" && !string.IsNullOrEmpty(trainName))
        {
            var halfWidth = ToHalfWidth(trainName);
            if (trainTypeIdByName.TryGetValue(halfWidth, out var id2))
            {
                return id2;
            }
        }

        logger.LogWarning("列車種別 {TrainClass} が見つかりません。デフォルト値 1 を使用します。", trainClass);
        return 1;
    }

    // 全角ASCII変換(U+FF01-U+FF5E → U+0021-U+007E)
    private static string ToHalfWidth(string input) =>
        new(input.Select(c => c is >= '！' and <= '～' ? (char)(c - 0xFEE0) : c).ToArray());

    /// <summary>
    ///     Convert TimeOfDay to TimeSpan
    /// </summary>
    private TimeSpan? ConvertToTimeSpan(TimeOfDay? timeOfDay)
    {
        if (timeOfDay == null)
        {
            return null;
        }

        return new TimeSpan(timeOfDay.h, timeOfDay.m, timeOfDay.s);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // This method is not used for TrainDbInitializer as it requires CSV data
        // Use InitializeTrainTypesAsync and InitializeTrainDiagramsAsync instead
        await Task.CompletedTask;
    }
}
