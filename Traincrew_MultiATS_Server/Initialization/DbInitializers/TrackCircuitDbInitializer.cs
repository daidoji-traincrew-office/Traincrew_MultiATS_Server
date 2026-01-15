using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Signal;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Traincrew_MultiATS_Server.Repositories.TrackCircuitSignal;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes track circuit entities in the database
/// </summary>
public class TrackCircuitDbInitializer(
    ILogger<TrackCircuitDbInitializer> logger,
    ITrackCircuitRepository trackCircuitRepository,
    ISignalRepository signalRepository,
    ITrackCircuitSignalRepository trackCircuitSignalRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer(logger)
{
    /// <summary>
    ///     Initialize track circuits from CSV data
    /// </summary>
    public async Task InitializeTrackCircuitsAsync(List<TrackCircuitCsv> trackCircuitList,
        CancellationToken cancellationToken = default)
    {
        var trackCircuitNames = (await trackCircuitRepository.GetAllNames(cancellationToken)).ToHashSet();

        var trackCircuits = new List<TrackCircuit>();
        foreach (var item in trackCircuitList)
        {
            if (trackCircuitNames.Contains(item.Name))
            {
                continue;
            }

            trackCircuits.Add(new()
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
        }

        await generalRepository.AddAll(trackCircuits, cancellationToken);
        _logger.LogInformation("Initialized {Count} track circuits", trackCircuits.Count);
    }

    /// <summary>
    ///     Initialize track circuit signal relationships from CSV data
    /// </summary>
    public async Task InitializeTrackCircuitSignalsAsync(List<TrackCircuitCsv> trackCircuitList,
        CancellationToken cancellationToken = default)
    {
        var trackCircuitNames = trackCircuitList.Select(tc => tc.Name).ToHashSet();
        var trackCircuitEntities = await trackCircuitRepository.GetTrackCircuitsByNamesAsync(
            trackCircuitNames, cancellationToken);

        var allSignalNames = trackCircuitList
            .SelectMany(tc => tc.NextSignalNamesUp.Concat(tc.NextSignalNamesDown))
            .Distinct()
            .ToHashSet();
        var signals = await signalRepository.GetSignalsByNamesAsync(allSignalNames, cancellationToken);

        var trackCircuitIds = trackCircuitEntities.Values.Select(tc => tc.Id).ToHashSet();
        var existingRelationsSet = await trackCircuitSignalRepository.GetExistingRelations(
            trackCircuitIds, cancellationToken);

        var trackCircuitSignals = new List<TrackCircuitSignal>();
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

                trackCircuitSignals.Add(new()
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

                trackCircuitSignals.Add(new()
                {
                    TrackCircuitId = trackCircuitEntity.Id,
                    SignalName = signal.Name,
                    IsUp = false
                });
            }
        }

        await generalRepository.AddAll(trackCircuitSignals, cancellationToken);
        _logger.LogInformation("Initialized track circuit signals");
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // This method is not used for TrackCircuitDbInitializer as it requires CSV data
        // Use InitializeTrackCircuitsAsync and InitializeTrackCircuitSignalsAsync instead
        await Task.CompletedTask;
    }
}