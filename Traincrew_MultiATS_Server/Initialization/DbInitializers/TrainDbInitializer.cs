using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.TrainDiagram;
using Traincrew_MultiATS_Server.Repositories.TrainType;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes train type and train diagram entities in the database
/// </summary>
public class TrainDbInitializer(
    ILogger<TrainDbInitializer> logger,
    ITrainTypeRepository trainTypeRepository,
    ITrainDiagramRepository trainDiagramRepository,
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

        var trainTypes = new List<TrainType>();
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
    public async Task InitializeTrainDiagramsAsync(List<TrainDiagramCsv> trainDiagramList,
        CancellationToken cancellationToken = default)
    {
        var existingNumbers = await trainDiagramRepository.GetTrainNumbersForAll(cancellationToken);

        var trainDiagrams = new List<TrainDiagram>();
        foreach (var record in trainDiagramList)
        {
            if (existingNumbers.Contains(record.TrainNumber))
            {
                continue;
            }

            trainDiagrams.Add(new()
            {
                TrainNumber = record.TrainNumber,
                TrainTypeId = record.TypeId,
                FromStationId = record.FromStationId,
                ToStationId = record.ToStationId,
                DiaId = record.DiaId
            });
        }

        await generalRepository.AddAll(trainDiagrams, cancellationToken);
        logger.LogInformation("Initialized {Count} train diagrams", trainDiagrams.Count);
    }

    /// <summary>
    ///     Initialize train diagrams and timetables from TTC_Data
    /// </summary>
    public async Task InitializeFromTtcDataAsync(TTC_Data ttcData, int diaId = 1, CancellationToken cancellationToken = default)
    {
        // 既存のTrainDiagramをdiaIdで取得
        var existingDiagrams = await trainDiagramRepository.GetForTrainNumberByDiaId(diaId, cancellationToken);
        var trainTypeIdByName = await trainTypeRepository.GetAllIdForName(cancellationToken);

        // diaIdに紐づくTimetablesを全削除 (ExecuteDelete)
        await trainDiagramRepository.DeleteTimetablesByDiaId(diaId, cancellationToken);
        logger.LogInformation("Deleted existing timetables for diaId {DiaId}", diaId);

        var trainDiagramsToAdd = new List<TrainDiagram>();
        var trainDiagramsToUpdate = new List<TrainDiagram>();
        var trainTimetablesByTrainNumber = new Dictionary<string, List<TrainDiagramTimetable>>();

        foreach (var ttcTrain in ttcData.trainList)
        {
            if (string.IsNullOrWhiteSpace(ttcTrain.trainNumber))
            {
                logger.LogWarning("列車番号が空です。{operationNumber} スキップします。", ttcTrain.operationNumber);
                continue;
            }

            // Get train type ID from train class name, default to 1 if not found
            var trainTypeId = trainTypeIdByName.GetValueOrDefault(ttcTrain.trainClass, 1);

            var newDiagram = new TrainDiagram
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

            // Create TrainDiagramTimetable for each TTC_StationData (IDは後で設定)
            var timetables = ttcTrain.staList.Select((stationData, i) => new TrainDiagramTimetable()
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
        var allDiagrams = await trainDiagramRepository.GetForTrainNumberByDiaId(diaId, cancellationToken);
        var trainTimetables = new List<TrainDiagramTimetable>();

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

        logger.LogInformation("Completed initialization from TTC_Data: {AddCount} added, {UpdateCount} updated train diagrams, {TimetableCount} timetables",
            trainDiagramsToAdd.Count, trainDiagramsToUpdate.Count, trainTimetables.Count);
    }

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