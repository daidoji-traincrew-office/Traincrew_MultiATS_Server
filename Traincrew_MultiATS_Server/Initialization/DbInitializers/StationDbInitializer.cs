using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes station and station timer state entities in the database
/// </summary>
public class StationDbInitializer(ApplicationDbContext context, ILogger<StationDbInitializer> logger)
    : BaseDbInitializer(context, logger)
{
    /// <summary>
    ///     Initialize stations from CSV data
    /// </summary>
    public async Task InitializeStationsAsync(List<StationCsv> stationList,
        CancellationToken cancellationToken = default)
    {
        var stationNames = (await _context.Stations
            .Select(s => s.Name)
            .ToListAsync(cancellationToken)).ToHashSet();

        var addedCount = 0;
        foreach (var station in stationList)
        {
            if (stationNames.Contains(station.Name))
            {
                continue;
            }

            _context.Stations.Add(new()
            {
                Id = station.Id,
                Name = station.Name,
                IsStation = station.IsStation,
                IsPassengerStation = station.IsPassengerStation
            });
            addedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} stations", addedCount);
    }

    /// <summary>
    ///     Initialize station timer states (30s and 60s timers for each station)
    /// </summary>
    public async Task InitializeStationTimerStatesAsync(CancellationToken cancellationToken = default)
    {
        var stationIds = (await _context.Stations
            .Where(s => s.IsStation)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken)).ToHashSet();

        var stationTimerStates = (await _context.StationTimerStates
            .Select(s => new { s.StationId, s.Seconds })
            .ToListAsync(cancellationToken)).ToHashSet();

        var addedCount = 0;
        foreach (var stationId in stationIds)
        {
            foreach (var seconds in new[] { 30, 60 })
            {
                if (stationTimerStates.Contains(new { StationId = stationId, Seconds = seconds }))
                {
                    continue;
                }

                _context.StationTimerStates.Add(new()
                {
                    StationId = stationId,
                    Seconds = seconds,
                    IsTeuRelayRaised = RaiseDrop.Drop,
                    IsTenRelayRaised = RaiseDrop.Drop,
                    IsTerRelayRaised = RaiseDrop.Raise
                });
                addedCount++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} station timer states", addedCount);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // This method is not used for StationDbInitializer as it requires CSV data
        // Use InitializeStationsAsync and InitializeStationTimerStatesAsync instead
        await Task.CompletedTask;
    }
}