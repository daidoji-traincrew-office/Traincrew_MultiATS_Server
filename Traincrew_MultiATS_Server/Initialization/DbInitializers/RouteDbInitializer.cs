using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes route lock track circuit relationships from CSV data
/// </summary>
public class RouteDbInitializer(ApplicationDbContext context, ILogger<RouteDbInitializer> logger)
    : BaseDbInitializer(context, logger)
{
    /// <summary>
    ///     Initialize route lock track circuits from CSV file (進路.csv)
    /// </summary>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var file = new FileInfo("./Data/進路.csv");
        if (!file.Exists)
        {
            _logger.LogWarning("Route CSV file not found: {FilePath}", file.FullName);
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
        csv.Context.RegisterClassMap<RouteLockTrackCircuitCsvMap>();
        var records = await csv
            .GetRecordsAsync<RouteLockTrackCircuitCsv>(cancellationToken)
            .ToListAsync(cancellationToken);
        var routes = await _context.Routes
            .Select(r => new { r.Name, r.Id })
            .ToDictionaryAsync(r => r.Name, r => r.Id, cancellationToken);
        var trackCircuits = await _context.TrackCircuits
            .Select(tc => new { tc.Name, tc.Id })
            .ToDictionaryAsync(tc => tc.Name, tc => tc.Id, cancellationToken);
        var routeLockTrackCircuits = (await _context.RouteLockTrackCircuits
            .Select(r => new { r.RouteId, r.TrackCircuitId })
            .ToListAsync(cancellationToken)).ToHashSet();

        var addedCount = 0;
        foreach (var record in records)
        {
            // 該当進路が登録されていない場合スキップ
            if (!routes.TryGetValue(record.RouteName, out var routeId)) continue;

            foreach (var trackCircuitName in record.TrackCircuitNames)
            {
                // 該当軌道回路が登録されていない場合スキップ
                if (!trackCircuits.TryGetValue(trackCircuitName, out var trackCircuitId)) continue;

                // 既に登録済みの場合、スキップ
                if (routeLockTrackCircuits.Contains(new { RouteId = routeId, TrackCircuitId = trackCircuitId }))
                    continue;

                _context.RouteLockTrackCircuits.Add(new()
                {
                    RouteId = routeId,
                    TrackCircuitId = trackCircuitId
                });
                addedCount++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} route lock track circuits", addedCount);
    }
}