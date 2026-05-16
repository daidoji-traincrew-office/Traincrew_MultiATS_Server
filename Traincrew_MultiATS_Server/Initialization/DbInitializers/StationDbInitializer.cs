using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Station;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes station and station timer state entities in the database
/// </summary>
public class StationDbInitializer(
    ILogger<StationDbInitializer> logger,
    IStationRepository stationRepository,
    IStationTimerStateRepository stationTimerStateRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer
{
    /// <summary>
    ///     Initialize stations from CSV data
    /// </summary>
    public async Task InitializeStationsAsync(List<StationCsv> stationList,
        CancellationToken cancellationToken = default)
    {
        var stationNames = (await stationRepository.GetAllNames(cancellationToken)).ToHashSet();

        var stations = new List<Station>();
        foreach (var station in stationList)
        {
            if (stationNames.Contains(station.Name))
            {
                continue;
            }

            stations.Add(new()
            {
                Id = station.Id,
                Name = station.Name,
                IsStation = station.IsStation,
                IsPassengerStation = station.IsPassengerStation
            });
        }

        await generalRepository.AddAll(stations, cancellationToken);
        logger.LogInformation("Initialized {Count} stations", stations.Count);
    }

    /// <summary>
    ///     Initialize station timer states (30s and 60s timers for each station)
    /// </summary>
    public async Task InitializeStationTimerStatesAsync(CancellationToken cancellationToken = default)
    {
        var stationIds = (await stationRepository.GetIdsWhereIsStation(cancellationToken)).ToHashSet();

        var stationTimerStates = await stationTimerStateRepository.GetExistingTimerStates(cancellationToken);

        var timerStates = new List<StationTimerState>();
        foreach (var stationId in stationIds)
        {
            foreach (var seconds in new[] { 30, 60 })
            {
                if (stationTimerStates.Contains((stationId, seconds)))
                {
                    continue;
                }

                timerStates.Add(new()
                {
                    StationId = stationId,
                    Seconds = seconds,
                    IsTeuRelayRaised = RaiseDrop.Drop,
                    IsTenRelayRaised = RaiseDrop.Drop,
                    IsTerRelayRaised = RaiseDrop.Raise
                });
            }
        }

        await generalRepository.AddAll(timerStates, cancellationToken);
        logger.LogInformation("Initialized {Count} station timer states", timerStates.Count);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // This method is not used for StationDbInitializer as it requires CSV data
        // Use InitializeStationsAsync and InitializeStationTimerStatesAsync instead
        await Task.CompletedTask;
    }
}