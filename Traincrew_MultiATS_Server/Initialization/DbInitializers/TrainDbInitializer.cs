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
        var existingNumbers = await trainDiagramRepository.GetTrainNumbersForAll(cancellationToken);
        var trainTypeIdByName = await trainTypeRepository.GetAllIdForName(cancellationToken);

        var trainDiagrams = new List<TrainDiagram>();
        var trainTimetables = new List<TrainDiagramTimetable>();

        foreach (var ttcTrain in ttcData.trainList)
        {
            if (string.IsNullOrWhiteSpace(ttcTrain.trainNumber))
            {
                logger.LogWarning("列車番号が空です。{operationNumber} スキップします。", ttcTrain.operationNumber);
                continue;
            }
            // Skip if train diagram already exists
            if (existingNumbers.Contains(ttcTrain.trainNumber))
            {
                continue;
            }

            // Get train type ID from train class name, default to 1 if not found
            var trainTypeId = trainTypeIdByName.GetValueOrDefault(ttcTrain.trainClass, 1);

            // Create TrainDiagram for each TTC_Train
            trainDiagrams.Add(new()
            {
                TrainNumber = ttcTrain.trainNumber,
                TrainTypeId = trainTypeId,
                FromStationId = ttcTrain.originStationID,
                ToStationId = ttcTrain.destinationStationID,
                DiaId = diaId
            });

            // Create TrainDiagramTimetable for each TTC_StationData
            trainTimetables.AddRange(ttcTrain.staList.Select((stationData, i) => new TrainDiagramTimetable()
            {
                TrainNumber = ttcTrain.trainNumber,
                Index = i + 1,
                StationId = stationData.stationID,
                TrackNumber = stationData.stopPosName ?? "",
                ArrivalTime = ConvertToTimeSpan(stationData.arrivalTime),
                DepartureTime = ConvertToTimeSpan(stationData.departureTime)
            }));
        }

        await generalRepository.AddAll(trainDiagrams, cancellationToken);
        await generalRepository.AddAll(trainTimetables, cancellationToken);

        logger.LogInformation("Initialized {TrainCount} train diagrams and {TimetableCount} timetables from TTC_Data",
            trainDiagrams.Count, trainTimetables.Count);
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