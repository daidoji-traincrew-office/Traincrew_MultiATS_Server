using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes train type and train diagram entities in the database
/// </summary>
public class TrainDbInitializer(ApplicationDbContext context, ILogger<TrainDbInitializer> logger)
    : BaseDbInitializer(context, logger)
{
    /// <summary>
    ///     Initialize train types from CSV data
    /// </summary>
    public async Task InitializeTrainTypesAsync(List<TrainTypeCsv> trainTypeList,
        CancellationToken cancellationToken = default)
    {
        var existingIds = await _context.TrainTypes.Select(t => t.Id).ToListAsync(cancellationToken);

        var addedCount = 0;
        foreach (var record in trainTypeList)
        {
            if (existingIds.Contains(record.Id)) continue;

            _context.TrainTypes.Add(new()
            {
                Id = record.Id,
                Name = record.Name
            });
            addedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} train types", addedCount);
    }

    /// <summary>
    ///     Initialize train diagrams from CSV data
    /// </summary>
    public async Task InitializeTrainDiagramsAsync(List<TrainDiagramCsv> trainDiagramList,
        CancellationToken cancellationToken = default)
    {
        var existingNumbers = await _context.TrainDiagrams.Select(t => t.TrainNumber).ToListAsync(cancellationToken);

        var addedCount = 0;
        foreach (var record in trainDiagramList)
        {
            if (existingNumbers.Contains(record.TrainNumber)) continue;

            _context.TrainDiagrams.Add(new()
            {
                TrainNumber = record.TrainNumber,
                TrainTypeId = record.TypeId,
                FromStationId = record.FromStationId,
                ToStationId = record.ToStationId,
                DiaId = record.DiaId
            });
            addedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} train diagrams", addedCount);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // This method is not used for TrainDbInitializer as it requires CSV data
        // Use InitializeTrainTypesAsync and InitializeTrainDiagramsAsync instead
        await Task.CompletedTask;
    }
}