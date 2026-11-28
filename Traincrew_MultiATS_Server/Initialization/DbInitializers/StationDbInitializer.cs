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
        var existingStationByName = (await stationRepository.GetAllAsync(cancellationToken))
            .ToDictionary(s => s.Name);

        var stations = new List<Station>();
        var stationsToUpdate = new List<Station>();
        foreach (var station in stationList)
        {
            if (existingStationByName.TryGetValue(station.Name, out var existingStation))
            {
                // 既存の駅は、駅制御装置ブロックだけCSVに揃える(追加時にしか入らないと、既存DBの駅に入らない)
                if (existingStation.ControlBlock != station.ControlBlock)
                {
                    existingStation.ControlBlock = station.ControlBlock;
                    stationsToUpdate.Add(existingStation);
                }

                continue;
            }

            stations.Add(new()
            {
                Id = station.Id,
                Name = station.Name,
                IsStation = station.IsStation,
                IsPassengerStation = station.IsPassengerStation,
                ControlBlock = station.ControlBlock
            });
        }

        await generalRepository.SaveAll(stationsToUpdate, cancellationToken);
        await generalRepository.AddAll(stations, cancellationToken);
        logger.LogInformation("Initialized {Count} stations, updated {UpdatedCount} station control blocks",
            stations.Count, stationsToUpdate.Count);
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

                // IsTimerConditionMet は常に false で初期化される
                // (リレー初期値のため)
                // リレー初期値自体は連動側(Traincrew_Rendo_Server)の初期化処理が付与する
                timerStates.Add(new()
                {
                    StationId = stationId,
                    Seconds = seconds,
                    IsTimerConditionMet = false
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