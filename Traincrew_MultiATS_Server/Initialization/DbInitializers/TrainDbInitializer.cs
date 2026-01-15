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
    : BaseDbInitializer(logger)
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
        _logger.LogInformation("Initialized {Count} train types", trainTypes.Count);
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
        _logger.LogInformation("Initialized {Count} train diagrams", trainDiagrams.Count);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // This method is not used for TrainDbInitializer as it requires CSV data
        // Use InitializeTrainTypesAsync and InitializeTrainDiagramsAsync instead
        await Task.CompletedTask;
    }
}