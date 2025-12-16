using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes TTC (Train Traffic Control) window and window link entities in the database
/// </summary>
public class TtcDbInitializer(ApplicationDbContext context, ILogger<TtcDbInitializer> logger)
    : BaseDbInitializer(context, logger)
{
    /// <summary>
    ///     Initialize TTC windows from CSV file (TTC列番窓.csv)
    /// </summary>
    public async Task InitializeTtcWindowsAsync(CancellationToken cancellationToken = default)
    {
        var file = new FileInfo("./Data/TTC列番窓.csv");
        if (!file.Exists)
        {
            _logger.LogWarning("TTC windows CSV file not found: {FilePath}", file.FullName);
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
        csv.Context.RegisterClassMap<TtcWindowCsvMap>();
        var records = await csv
            .GetRecordsAsync<TtcWindowCsv>(cancellationToken)
            .ToListAsync(cancellationToken);

        var existingWindows = await _context.TtcWindows
            .Select(w => w.Name)
            .AsAsyncEnumerable()
            .ToHashSetAsync(cancellationToken);
        var trackCircuitIdByName = await _context.TrackCircuits
            .ToDictionaryAsync(tc => tc.Name, tc => tc.Id, cancellationToken);

        var addedCount = 0;
        foreach (var record in records)
        {
            if (existingWindows.Contains(record.Name)) continue;

            _context.TtcWindows.Add(new()
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
                _context.TtcWindowDisplayStations.Add(new()
                {
                    TtcWindowName = record.Name,
                    StationId = displayStation
                });
            }

            foreach (var trackCircuit in record.TrackCircuits)
            {
                _context.TtcWindowTrackCircuits.Add(new()
                {
                    TtcWindowName = record.Name,
                    TrackCircuitId = trackCircuitIdByName[trackCircuit]
                });
            }

            addedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} TTC windows", addedCount);
    }

    /// <summary>
    ///     Initialize TTC window links from CSV file (TTC列番窓リンク設定.csv)
    /// </summary>
    public async Task InitializeTtcWindowLinksAsync(CancellationToken cancellationToken = default)
    {
        var file = new FileInfo("./Data/TTC列番窓リンク設定.csv");
        if (!file.Exists)
        {
            _logger.LogWarning("TTC window links CSV file not found: {FilePath}", file.FullName);
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
        csv.Context.RegisterClassMap<TtcWindowLinkCsvMap>();
        var records = await csv
            .GetRecordsAsync<TtcWindowLinkCsv>(cancellationToken).ToListAsync(cancellationToken);

        var existingLinks = await _context.TtcWindowLinks
            .Select(l => new { Source = l.SourceTtcWindowName, Target = l.TargetTtcWindowName })
            .AsAsyncEnumerable()
            .ToHashSetAsync(cancellationToken);
        var trackCircuitIdByName = await _context.TrackCircuits
            .ToDictionaryAsync(tc => tc.Name, tc => tc.Id, cancellationToken);
        var routeIdByName = await _context.Routes
            .ToDictionaryAsync(r => r.Name, r => r.Id, cancellationToken);

        var addedCount = 0;
        foreach (var record in records)
        {
            if (existingLinks.Contains(new { record.Source, record.Target })) continue;

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
            _context.TtcWindowLinks.Add(ttcWindowLink);

            foreach (var routeCondition in record.RouteConditions)
            {
                if (!routeIdByName.TryGetValue(routeCondition, out var routeId)) continue;

                _context.TtcWindowLinkRouteConditions.Add(new()
                {
                    RouteId = routeId,
                    TtcWindowLink = ttcWindowLink
                });
            }

            addedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} TTC window links", addedCount);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await InitializeTtcWindowsAsync(cancellationToken);
        await InitializeTtcWindowLinksAsync(cancellationToken);
    }
}