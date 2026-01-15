using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.OperationNotificationDisplay;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes operation notification display entities in the database
/// </summary>
public class OperationNotificationDisplayDbInitializer(
    ILogger<OperationNotificationDisplayDbInitializer> logger,
    IDateTimeRepository dateTimeRepository,
    IOperationNotificationDisplayRepository operationNotificationDisplayRepository,
    ITrackCircuitRepository trackCircuitRepository,
    OperationNotificationDisplayCsvLoader csvLoader,
    IGeneralRepository generalRepository)
    : BaseDbInitializer(logger)
{
    /// <summary>
    ///     Initialize operation notification displays from CSV file (運転告知器.csv)
    /// </summary>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var records = await csvLoader.LoadAsync(cancellationToken);
        var trackCircuitNames = records
            .SelectMany(r => r.TrackCircuitNames)
            .ToList();
        var trackCircuits = await trackCircuitRepository.GetByNames(trackCircuitNames, cancellationToken);
        var operationNotificationDisplayNames = await operationNotificationDisplayRepository.GetAllNames(cancellationToken);

        var newDisplays = new List<OperationNotificationDisplay>();
        var updatedTrackCircuits = new List<TrackCircuit>();

        foreach (var record in records)
        {
            var name = record.Name;
            if (operationNotificationDisplayNames.Contains(name))
            {
                continue;
            }

            newDisplays.Add(new()
            {
                Name = name,
                StationId = record.StationId,
                IsUp = record.IsUp,
                IsDown = record.IsDown,
                OperationNotificationState = new()
                {
                    DisplayName = name,
                    Type = OperationNotificationType.None,
                    Content = "",
                    OperatedAt = dateTimeRepository.GetNow().AddDays(-1)
                }
            });

            foreach (var trackCircuitName in record.TrackCircuitNames)
            {
                if (!trackCircuits.TryGetValue(trackCircuitName, out var trackCircuit))
                {
                    continue;
                }

                trackCircuit.OperationNotificationDisplayName = name;
                updatedTrackCircuits.Add(trackCircuit);
            }
        }

        await generalRepository.AddAll(newDisplays);
        await generalRepository.SaveAll(updatedTrackCircuits);
        _logger.LogInformation("Initialized {Count} operation notification displays", newDisplays.Count);
    }
}