using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Repositories.Station;
using Traincrew_MultiATS_Server.Repositories.TtcWindow;
using Route = Traincrew_MultiATS_Server.Models.Route;
using RouteData = Traincrew_MultiATS_Server.Common.Models.RouteData;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 配信が1ティックにつき1回だけ読んだserver_stateの値。不変。
/// </summary>
/// <remarks>
/// IsAllSignalRelayRaisedと連動のハートビートは別プロセス(連動サーバー)が書く。
/// そのため CacheGate は通さず、スナップショットのトランザクション内でDBから直接読んだ値を使う。
/// </remarks>
public sealed record ServerStateSnapshot(
    ServerMode Mode,
    int TimeOffset,
    ulong? SelectedDiagramId,
    RaiseDropWithForce? IsAllSignalRelayRaised,
    DateTime? InterlockingHeartbeatAt)
{
    public static ServerStateSnapshot From(ServerState? serverState)
    {
        // 行が無い場合は旧実装(GetTimeOffset ?? 0, GetSelectedDiagramId = null, ヘルス判定は不健全)と同じ
        return new(
            serverState?.Mode ?? ServerMode.Off,
            serverState?.TimeOffset ?? 0,
            serverState?.SelectedDiagramId,
            serverState?.IsAllSignalRelayRaised,
            serverState?.InterlockingHeartbeatAt);
    }
}

/// <summary>
/// 信号現示計算の入力。毎ティック新しく組み立てる(共有しない)。
/// マスタ・連動オブジェクトの共有インスタンスは含まず、Signalは配信用に組み立て直した新規インスタンス。
/// </summary>
public sealed record SignalCalcInput(
    Dictionary<string, Signal> Signals,
    Dictionary<string, List<string>> NextSignalNames,
    Dictionary<string, List<Route>> Routes);

/// <summary>
/// 配信系サービスが共通して読み取るデータをまとめたもの。
/// 1ティック・1トランザクション・1回の読み取りで作り、全ハブで共有する。
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
    ServerStateSnapshot ServerState,
    List<InterlockingLeverData> Levers,
    List<DirectionSelfControlLever> DirectionSelfControlLevers,
    List<DestinationButtonData> DestinationButtons,
    List<RouteData> ActiveRoutes,
    SignalCalcInput SignalInput)
{
    public int TimeOffset => ServerState.TimeOffset;
}

/// <summary>
/// <see cref="CommonReads"/> を組み立てる。
/// <see cref="BroadcastSnapshotService"/> がプッシュ配信(<c>ReceiveData</c>)のために各Build*Asyncを呼ぶ際に使う、
/// 共通読み取りロジックの唯一の実装。
/// </summary>
public interface ICommonReadsBuilder
{
    Task<CommonReads> BuildAsync();
}

/// <summary>
/// マスタ(起動時にロード済みの不変スナップショット)と、毎ティック読む *_state から配信用データを組み立てる。
/// </summary>
/// <remarks>
/// 連動オブジェクトは <see cref="IInterlockingObjectStateLoader"/> が状態込みの複製を返し、
/// 既存の変換メソッド(ToSwitchData等)をそのまま使う。同じIDのStateは1ティックで1インスタンスで、
/// 信号現示の入力もそのインスタンスを参照する。マスタストアの Current はティックの開始時に1回だけ読む。
/// </remarks>
public class CommonReadsBuilder(
    IBroadcastMasterStore broadcastMasterStore,
    IInterlockingObjectStateLoader interlockingObjectStateLoader,
    IServerRepository serverRepository,
    IStationRepository stationRepository,
    ITtcWindowRepository ttcWindowRepository,
    IDestinationButtonRepository destinationButtonRepository,
    ITrainService trainService) : ICommonReadsBuilder
{
    public async Task<CommonReads> BuildAsync()
    {
        // ティックの開始時に1回だけ読む(途中で読み直さない)
        var master = broadcastMasterStore.Current;

        var serverState = ServerStateSnapshot.From(await serverRepository.GetServerStateForBroadcastAsync());
        var objects = await interlockingObjectStateLoader.GetAllWithStateForBroadcast();
        var ttcWindowStates = (await ttcWindowRepository.GetAllStatesForBroadcast())
            .ToDictionary(state => state.Name);
        var destinationButtonStates = (await destinationButtonRepository.GetAllStatesForBroadcast())
            .ToDictionary(state => state.Name);
        var allStationTimerStates = await stationRepository.GetAllTimerStatesForBroadcast();
        var trainStates = await trainService.GetAllTrainState();

        var stationIds = master.StationIds.ToList();
        var stationIdSet = stationIds.ToHashSet();

        return new(
            stationIds,
            // 状態行が無い軌道回路は含めない(旧実装は状態行から引いていた)。
            // 状態行が無いときは複製にnullが載る(型の注釈上は非nullだが実際にはあり得る)ので、nullチェックは必要
            objects.TrackCircuits
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                .Where(trackCircuit => trackCircuit.TrackCircuitState != null)
                .Select(TrackCircuitService.ToTrackCircuitData)
                .ToList(),
            objects.SwitchingMachines.Select(SwitchingMachineService.ToSwitchData).ToList(),
            objects.DirectionRoutes.Select(DirectionRouteService.ToDirectionData).ToList(),
            trainStates,
            objects.RouteCentralControlLevers.ToList(),
            BuildTtcWindows(master, stationIdSet, ttcWindowStates),
            allStationTimerStates.Where(state => stationIdSet.Contains(state.StationId)).ToList(),
            serverState,
            objects.Levers.Select(LeverService.ToLeverData).ToList(),
            objects.DirectionSelfControlLevers.ToList(),
            master.DestinationButtons
                .Select(button => DestinationButtonService.ToDestinationButtonData(
                    destinationButtonStates.GetValueOrDefault(button.Name)!))
                .ToList(),
            // 有効進路: 進路確保(IsRouteSecured)が扛上している進路
            objects.Routes
                .Where(route => route.RouteState?.IsRouteSecured == RaiseDrop.Raise)
                .Select(RouteService.ToRouteData)
                .ToList(),
            BuildSignalInput(master, objects));
    }

    /// <summary>
    /// 所属駅の他に表示駅も含める(旧 GetTtcWindowsByStationIdsWithState と同じ絞り込み)。
    /// </summary>
    private static List<TtcWindow> BuildTtcWindows(
        BroadcastMasterData master,
        HashSet<string> stationIdSet,
        Dictionary<string, TtcWindowState> ttcWindowStates)
    {
        return master.TtcWindows
            .Where(window => stationIdSet.Contains(window.StationId)
                             || window.DisplayStationIds.Any(stationIdSet.Contains))
            .Select(window => new TtcWindow
            {
                Name = window.Name,
                StationId = window.StationId,
                Type = window.Type,
                TtcWindowState = ttcWindowStates.GetValueOrDefault(window.Name)
            })
            .ToList();
    }

    /// <summary>
    /// 信号現示計算の入力を組み立てる。軌道回路・方向進路・進路は、同じティックで読んだ状態込みの複製を参照する
    /// (別に state を読み直さない)。
    /// </summary>
    private static SignalCalcInput BuildSignalInput(BroadcastMasterData master, InterlockingObjectsWithState objects)
    {
        var signals = master.Signals.ToDictionary(
            signal => signal.Name,
            signal => new Signal
            {
                Name = signal.Name,
                TypeName = signal.TypeName,
                StationId = signal.StationId,
                Type = signal.Type,
                TrackCircuitId = signal.TrackCircuitId,
                TrackCircuit = signal.TrackCircuitId is { } trackCircuitId
                    ? objects.TrackCircuitsById.GetValueOrDefault(trackCircuitId)
                    : null,
                DirectionRouteLeftId = signal.DirectionRouteLeftId,
                DirectionRouteLeft = signal.DirectionRouteLeftId is { } leftId
                    ? objects.DirectionRoutesById.GetValueOrDefault(leftId)
                    : null,
                DirectionRouteRightId = signal.DirectionRouteRightId,
                DirectionRouteRight = signal.DirectionRouteRightId is { } rightId
                    ? objects.DirectionRoutesById.GetValueOrDefault(rightId)
                    : null,
                Direction = signal.Direction
            });

        // 加工して返すのでコピーする(マスタ側のリストは共有・読み取り専用)
        var nextSignalNames = master.NextSignalNamesBySignalName.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToList());

        var routes = new Dictionary<string, List<Route>>();
        foreach (var (signalName, routeIds) in master.RouteIdsBySignalName)
        {
            routes[signalName] = routeIds
                .Select(routeId => objects.RoutesById.GetValueOrDefault(routeId))
                .OfType<Route>()
                .ToList();
        }

        return new(signals, nextSignalNames, routes);
    }
}
