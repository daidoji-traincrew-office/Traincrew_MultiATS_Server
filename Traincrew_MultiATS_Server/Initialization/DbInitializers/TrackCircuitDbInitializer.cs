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
        foreach (var trackCircuit in trackCircuitList)
        {
            var trackCircuitEntity = await _context.TrackCircuits
                .FirstOrDefaultAsync(tc => tc.Name == trackCircuit.Name, cancellationToken);

            if (trackCircuitEntity == null)
            {
                continue;
            }

            foreach (var signalName in trackCircuit.NextSignalNamesUp)
            {
                // Todo: ここでN+1問題が発生しているので、改善したほうが良いかも
                var signal = await _context.Signals
                    .FirstOrDefaultAsync(s => s.Name == signalName, cancellationToken);
                if (signal == null)
                {
                    continue;
                }

                var exists = await _context.TrackCircuitSignals
                    .AnyAsync(tcs => tcs.TrackCircuitId == trackCircuitEntity.Id && tcs.SignalName == signal.Name,
                        cancellationToken);
                if (exists)
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
                var signal = await _context.Signals
                    .FirstOrDefaultAsync(s => s.Name == signalName, cancellationToken);
                if (signal == null)
                {
                    continue;
                }

                var exists = await _context.TrackCircuitSignals
                    .AnyAsync(tcs => tcs.TrackCircuitId == trackCircuitEntity.Id && tcs.SignalName == signal.Name,
                        cancellationToken);
                if (exists)
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