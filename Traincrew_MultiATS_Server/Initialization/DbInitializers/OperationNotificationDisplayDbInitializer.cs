using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes operation notification display entities in the database
/// </summary>
public class OperationNotificationDisplayDbInitializer(
    ApplicationDbContext context,
    IDateTimeRepository dateTimeRepository,
    ILogger<OperationNotificationDisplayDbInitializer> logger)
    : BaseDbInitializer(context, logger)
{
    /// <summary>
    ///     Initialize operation notification displays from CSV file (運転告知器.csv)
    /// </summary>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var file = new FileInfo("./Data/運転告知器.csv");
        if (!file.Exists)
        {
            _logger.LogWarning("Operation notification display CSV file not found: {FilePath}", file.FullName);
            return;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        };
        using var reader = new StreamReader(file.FullName);
        // ヘッダー行を読み飛ばす
        await reader.ReadLineAsync(cancellationToken);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<OperationNotificationDisplayCsvMap>();
        var records = await csv
            .GetRecordsAsync<OperationNotificationDisplayCsv>(cancellationToken)
            .ToListAsync(cancellationToken);
        var trackCircuitNames = records
            .SelectMany(r => r.TrackCircuitNames)
            .ToList();
        var trackCircuits = await _context.TrackCircuits
            .Where(tc => trackCircuitNames.Contains(tc.Name))
            .ToDictionaryAsync(tc => tc.Name, cancellationToken);
        var operationNotificationDisplayNames = await _context.OperationNotificationDisplays
            .Select(ond => ond.Name)
            .ToListAsync(cancellationToken);
        List<TrackCircuit> changedTrackCircuits = [];

        var addedCount = 0;
        foreach (var record in records)
        {
            var name = record.Name;
            if (operationNotificationDisplayNames.Contains(name)) continue;

            _context.OperationNotificationDisplays.Add(new()
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
                if (!trackCircuits.TryGetValue(trackCircuitName, out var trackCircuit)) continue;

                trackCircuit.OperationNotificationDisplayName = name;
                _context.TrackCircuits.Update(trackCircuit);
                changedTrackCircuits.Add(trackCircuit);
            }

            addedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} operation notification displays", addedCount);

        foreach (var trackCircuit in changedTrackCircuits) _context.Entry(trackCircuit).State = EntityState.Detached;
    }
}