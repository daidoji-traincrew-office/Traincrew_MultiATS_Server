using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.Route;
using RouteData = Traincrew_MultiATS_Server.Common.Models.RouteData;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// CTCP装置卓
/// </summary>
public interface ICTCPService
{
    Task<DataToCTCP> BuildCtcpDataAsync(CommonReads commonReads);
    Task<RouteData> SetCtcRelay(string TcName, RaiseDrop raiseDrop);
}

/// <summary>
/// CTCP装置卓
/// </summary>
public class CTCPService(
    IRouteRepository routeRepository,
    IGeneralRepository generalRepository,
    IRouteService routeService,
    IMutexRepository mutexRepository) : ICTCPService
{
    /// <summary>
    /// <see cref="CommonReads"/> からCTCP配信用データを組み立てる。
    /// mutexもトランザクションも張らない(呼び出し元が既に管理している前提)。
    /// </summary>
    public Task<DataToCTCP> BuildCtcpDataAsync(CommonReads commonReads)
    {
        // 各ランプの状態を取得
        var lamps = GetLamps(commonReads.StationIds, commonReads.StationTimerStates);

        Dictionary<string, CenterControlState> centerControlStates = commonReads.RouteCentralControlLevers.ToDictionary(
            lever => lever.Name.Replace("_ROUTE_CTC_LEVER", ""),
            lever => lever.RouteCentralControlLeverState is { IsCenterControlled: true }
                ? CenterControlState.CenterControl
                : CenterControlState.StationControl);

        var response = new DataToCTCP
        {
            TrackCircuits = commonReads.TrackCircuits,

            CenterControlStates = centerControlStates,

            Retsubans = commonReads.TtcWindows
                .Select(ToRetsubanData)
                .ToList(),

            // 各ランプの状態
            Lamps = lamps,

            TimeOffset = commonReads.TimeOffset
        };

        return Task.FromResult(response);
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
        await routeRepository.SetIsCtcControlledByIds([route.Id], raiseDrop);

        //更新後の進路データを返す
        return new RouteData
        {
            TcName = route.TcName,
            RouteType = route.RouteType,
            RootId = route.RootId,
            Indicator = route.Indicator,
            ApproachLockTime = route.ApproachLockTime
        };
    }

    private static Dictionary<string, bool> GetLamps(List<string> stationIds, List<StationTimerState> stationTimerStates)
    {
        // Todo: 一旦仮でFalse
        var pwrFailure = stationIds.ToDictionary(
            stationId => $"{stationId}_PWR-FAILURE",
            _ => false);
        var ctcFailure = stationIds.ToDictionary(
            stationId => $"{stationId}_CTC-FAILURE",
            _ => false);
        // 駅の時素状態を取得
        var stationTimerStateLamps = stationTimerStates
            .ToDictionary(
                timerState => $"{timerState.StationId}_{timerState.Seconds}TEK",
                timerState => timerState.IsTimerConditionMet);

        return pwrFailure
            .Concat(ctcFailure)
            .Concat(stationTimerStateLamps)
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