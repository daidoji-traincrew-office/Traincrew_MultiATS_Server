using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes track circuit entities in the database
/// </summary>
public class TrackCircuitDbInitializer(ApplicationDbContext context, ILogger<TrackCircuitDbInitializer> logger)
    : BaseDbInitializer(context, logger)
{
    /// <summary>
    ///     Initialize track circuits from CSV data
    /// </summary>
    public async Task InitializeTrackCircuitsAsync(List<TrackCircuitCsv> trackCircuitList,
        CancellationToken cancellationToken = default)
    {
        var trackCircuitNames = (await _context.TrackCircuits
            .Select(tc => tc.Name)
            .ToListAsync(cancellationToken)).ToHashSet();

        var addedCount = 0;
        foreach (var item in trackCircuitList)
        {
            if (trackCircuitNames.Contains(item.Name))
            {
                continue;
            }

            _context.TrackCircuits.Add(new()
            {
                // Todo: ProtectionZoneの未定義部分がなくなったら、ProtectionZoneのデフォルト値の設定を解除
                ProtectionZone = item.ProtectionZone ?? 99,
                Name = item.Name,
                Type = ObjectType.TrackCircuit,
                TrackCircuitState = new()
                {
                    IsShortCircuit = false,
                    IsLocked = false,
                    TrainNumber = "",
                    IsCorrectionDropRelayRaised = RaiseDrop.Drop,
                    IsCorrectionRaiseRelayRaised = RaiseDrop.Drop,
                    DroppedAt = null,
                    RaisedAt = null
                }
            });
            addedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} track circuits", addedCount);
    }

    /// <summary>
    ///     Initialize track circuit signal relationships from CSV data
    /// </summary>
    public async Task InitializeTrackCircuitSignalsAsync(List<TrackCircuitCsv> trackCircuitList,
        CancellationToken cancellationToken = default)
    {
        var trackCircuitNames = trackCircuitList.Select(tc => tc.Name).ToHashSet();
        var trackCircuitEntities = await _context.TrackCircuits
            .Where(tc => trackCircuitNames.Contains(tc.Name))
            .ToDictionaryAsync(tc => tc.Name, cancellationToken);

        var allSignalNames = trackCircuitList
            .SelectMany(tc => tc.NextSignalNamesUp.Concat(tc.NextSignalNamesDown))
            .Distinct()
            .ToHashSet();
        var signals = await _context.Signals
            .Where(s => allSignalNames.Contains(s.Name))
            .ToDictionaryAsync(s => s.Name, cancellationToken);

        var trackCircuitIds = trackCircuitEntities.Values.Select(tc => tc.Id).ToHashSet();
        var existingRelations = await _context.TrackCircuitSignals
            .Where(tcs => trackCircuitIds.Contains(tcs.TrackCircuitId))
            .Select(tcs => new { tcs.TrackCircuitId, tcs.SignalName })
            .ToListAsync(cancellationToken);
        var existingRelationsSet = existingRelations
            .Select(r => (r.TrackCircuitId, r.SignalName))
            .ToHashSet();

        foreach (var trackCircuit in trackCircuitList)
        {
            if (!trackCircuitEntities.TryGetValue(trackCircuit.Name, out var trackCircuitEntity))
            {
                continue;
            }

            foreach (var signalName in trackCircuit.NextSignalNamesUp)
            {
                if (!signals.TryGetValue(signalName, out var signal))
                {
                    continue;
                }

                if (existingRelationsSet.Contains((trackCircuitEntity.Id, signal.Name)))
                {
                    continue;
                }

                _context.TrackCircuitSignals.Add(new()
                {
                    TrackCircuitId = trackCircuitEntity.Id,
                    SignalName = signal.Name,
                    IsUp = true
                });
            }

            foreach (var signalName in trackCircuit.NextSignalNamesDown)
            {
                if (!signals.TryGetValue(signalName, out var signal))
                {
                    continue;
                }

                if (existingRelationsSet.Contains((trackCircuitEntity.Id, signal.Name)))
                {
                    continue;
                }

                _context.TrackCircuitSignals.Add(new()
                {
                    TrackCircuitId = trackCircuitEntity.Id,
                    SignalName = signal.Name,
                    IsUp = false
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized track circuit signals");
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // This method is not used for TrackCircuitDbInitializer as it requires CSV data
        // Use InitializeTrackCircuitsAsync and InitializeTrackCircuitSignalsAsync instead
        await Task.CompletedTask;
    }
}