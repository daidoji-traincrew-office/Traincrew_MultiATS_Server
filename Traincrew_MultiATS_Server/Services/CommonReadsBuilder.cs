using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.RouteCentralControlLever;
using Traincrew_MultiATS_Server.Repositories.Station;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 配信系サービスが共通して読み取る9つのデータをまとめたもの
/// </summary>
public record CommonReads(
    List<string> StationIds,
    List<TrackCircuitData> TrackCircuits,
    List<SwitchData> Switches,
    List<DirectionData> Directions,
    List<TrainStateData> TrainStates,
    List<RouteCentralControlLever> RouteCentralControlLevers,
    List<TtcWindow> TtcWindows,
    List<StationTimerState> StationTimerStates,
    int TimeOffset);

/// <summary>
/// <see cref="CommonReads"/> を組み立てる。
/// <see cref="BroadcastSnapshotService"/> がプッシュ配信(<c>ReceiveData</c>)のために各Build*Asyncを呼ぶ際に使う、
/// 共通読み取りロジックの唯一の実装。
/// </summary>
public interface ICommonReadsBuilder
{
    Task<CommonReads> BuildAsync();
}

public class CommonReadsBuilder(
    IStationRepository stationRepository,
    ITrackCircuitService trackCircuitService,
    ISwitchingMachineService switchingMachineService,
    IDirectionRouteService directionRouteService,
    ITrainService trainService,
    IRouteCentralControlLeverRepository routeCentralControlLeverRepository,
    ITtcStationControlService ttcStationControlService,
    IServerService serverService) : ICommonReadsBuilder
{
    public async Task<CommonReads> BuildAsync()
    {
        var stations = await stationRepository.GetWhereIsStation();
        var stationIds = stations.Select(station => station.Id).ToList();
        var trackCircuits = await trackCircuitService.GetAllTrackCircuitDataList();
        var switches = await switchingMachineService.GetAllSwitchData();
        var directions = await directionRouteService.GetAllDirectionData();
        var trainStates = await trainService.GetAllTrainState();
        var routeCentralControlLevers = await routeCentralControlLeverRepository.GetAllWithState();
        var ttcWindows = await ttcStationControlService.GetTtcWindowsByStationIdsWithState(stationIds);
        var stationTimerStates = await stationRepository.GetTimerStatesByStationIds(stationIds);
        var timeOffset = await serverService.GetTimeOffsetAsync();

        return new CommonReads(
            stationIds,
            trackCircuits,
            switches,
            directions,
            trainStates,
            routeCentralControlLevers,
            ttcWindows,
            stationTimerStates,
            timeOffset);
    }
}
