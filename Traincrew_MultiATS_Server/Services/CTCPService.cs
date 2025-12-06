using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteCentralControlLever;
using Traincrew_MultiATS_Server.Repositories.Station;
using RouteData = Traincrew_MultiATS_Server.Common.Models.RouteData;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// CTCP装置卓
/// </summary>
public class CTCPService(
    IRouteRepository routeRepository,
    IGeneralRepository generalRepository,
    IStationRepository stationRepository,
    IRouteCentralControlLeverRepository routeCentralControlLeverRepository,
    TrackCircuitService trackCircuitService,
    TtcStationControlService ttcStationControlService,
    RouteService routeService,
    IMutexRepository mutexRepository,
    ServerService serverService)
{

    public async Task<DataToCTCP> SendData_CTCP()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));
        var stations = await stationRepository.GetWhereIsStation();
        var stationIds = stations.Select(station => station.Id).ToList();
        var trackCircuits = await trackCircuitService.GetAllTrackCircuitDataList();
        //var directionSelfControlLevers = await directionSelfControlLeverRepository.GetAllWithState();
        //var directions = await directionRouteService.GetAllDirectionData();

        // 各ランプの状態を取得
        var lamps = await GetLamps(stationIds);
        // 列番窓を取得
        var ttcWindows = await ttcStationControlService.GetTtcWindowsByStationIdsWithState(stationIds);

        // 駅扱いてこを取得
        var routeCentralControlLever = await routeCentralControlLeverRepository.GetAllWithState();


        Dictionary<string, CenterControlState> centerControlStates = routeCentralControlLever.ToDictionary(
            lever => lever.Name.Replace("_ROUTE_CTC_LEVER", ""),
            lever => lever.RouteCentralControlLeverState != null && lever.RouteCentralControlLeverState.IsChrRelayRaised == RaiseDrop.Raise
                ? CenterControlState.CenterControl
                : CenterControlState.StationControl);

        // 時差を取得
        var timeOffset = await serverService.GetTimeOffsetAsync();

        var response = new DataToCTCP
        {
            TrackCircuits = trackCircuits,

            RouteDatas = await routeService.GetAllRoutes(),

            CenterControlStates = centerControlStates,

            Retsubans = ttcWindows
                .Select(ToRetsubanData)
                .ToList(),

            // 各ランプの状態
            Lamps = lamps,

            TimeOffset = timeOffset
        };

        return response;
    }

    /// <summary>
    /// CTCリレーの状態を設定する
    /// </summary>
    /// <param name="TcName">進路名</param>    
    /// <param name="raiseDrop">リレー状態</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<RouteData> SetCtcRelay(string TcName, RaiseDrop raiseDrop)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));
        //進路名から進路を取得
        var routes = await routeRepository.GetByTcNameWithState(TcName);

        //進路が見つからなかった場合は例外をスロー
        if (routes.Count == 0)
        {
            throw new ArgumentException($"進路名 '{TcName}' に対応する進路が見つかりません。");
        }

        //進路が複数見つかった場合は例外をスロー
        if (routes.Count > 1)
        {
            throw new ArgumentException($"進路名 '{TcName}' に対応する進路が複数見つかりました。");
        }

        var route = routes[0];

        //進路のCTCリレー状態を更新
        route.RouteState.IsCtcRelayRaised = raiseDrop;

        //更新した進路を保存
        await generalRepository.Save(route.RouteState);

        //更新後の進路データを返す
        return new RouteData
        {
            TcName = route.TcName,
            RouteType = route.RouteType,
            RootId = route.RootId,
            Indicator = route.Indicator,
            ApproachLockTime = route.ApproachLockTime,
            RouteState = new RouteStateData
            {
                IsLeverRelayRaised = route.RouteState.IsLeverRelayRaised,
                IsRouteRelayRaised = route.RouteState.IsRouteRelayRaised,
                IsSignalControlRaised = route.RouteState.IsSignalControlRaised,
                IsApproachLockMRRaised = route.RouteState.IsApproachLockMRRaised,
                IsApproachLockMSRaised = route.RouteState.IsApproachLockMSRaised,
                IsRouteLockRaised = route.RouteState.IsRouteLockRaised,
                IsThrowOutXRRelayRaised = route.RouteState.IsThrowOutXRRelayRaised,
                IsThrowOutYSRelayRaised = route.RouteState.IsThrowOutYSRelayRaised,
                IsRouteRelayWithoutSwitchingMachineRaised = route.RouteState.IsRouteRelayWithoutSwitchingMachineRaised,
                IsThrowOutSRelayRaised = route.RouteState.IsThrowOutSRelayRaised,
                IsThrowOutXRelayRaised = route.RouteState.IsThrowOutXRelayRaised,
                IsCtcRelayRaised = route.RouteState.IsCtcRelayRaised
            }
        };
    }

    private async Task<Dictionary<string, bool>> GetLamps(List<string> stationIds)
    {
        // Todo: 一旦仮でFalse
        var pwrFailure = stationIds.ToDictionary(
            stationId => $"{stationId}_PWR-FAILURE",
            _ => false);
        var ctcFailure = stationIds.ToDictionary(
            stationId => $"{stationId}_CTC-FAILURE",
            _ => false);
        // 駅の時素状態を取得
        var stationTimerStates = (await stationRepository.GetTimerStatesByStationIds(stationIds))
            .ToDictionary(
                timerState => $"{timerState.StationId}_{timerState.Seconds}TEK",
                timerState => timerState is { IsTenRelayRaised: RaiseDrop.Drop, IsTerRelayRaised: RaiseDrop.Drop });

        return pwrFailure
            .Concat(ctcFailure)
            .Concat(stationTimerStates)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static InterlockingRetsubanData ToRetsubanData(TtcWindow ttcWindow)
    {
        return new()
        {
            Name = ttcWindow.Name,
            Retsuban = ttcWindow.TtcWindowState?.TrainNumber ?? "",
        };
    }
}