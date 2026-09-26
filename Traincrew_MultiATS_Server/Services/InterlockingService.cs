using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.Station;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 連動装置装置卓
/// </summary>
/// <remarks>
/// てこ/着点ボタン/開放てこ/CTC切り替えてこの実処理は各ドメインサービスに委譲する。
/// このクラスは連動卓向けの集約と、Hubのメソッドと1:1対応するファサードのみを持つ。
/// 連動装置のmutexはこのクラスでのみ取得すること(MutexRepositoryは再入不可)。
/// </remarks>
public interface IInterlockingService
{
    Task<DataToInterlocking> SendData_Interlocking();
    Task<InterlockingLeverData> SetPhysicalLeverData(InterlockingLeverData leverData);
    Task<InterlockingKeyLeverData> SetPhysicalKeyLeverData(InterlockingKeyLeverData keyLeverData, ulong? memberId);
    Task<DestinationButtonData> SetDestinationButtonState(DestinationButtonData buttonData);
    Task ResetRaisedButtonsAsync();
}

/// <inheritdoc cref="IInterlockingService"/>
public class InterlockingService(
    IStationRepository stationRepository,
    ILeverService leverService,
    IDestinationButtonService destinationButtonService,
    IDirectionSelfControlLeverService directionSelfControlLeverService,
    IRouteCentralControlLeverService routeCentralControlLeverService,
    ITrackCircuitService trackCircuitService,
    ITtcStationControlService ttcStationControlService,
    ISwitchingMachineService switchingMachineService,
    IDirectionRouteService directionRouteService,
    IMutexRepository mutexRepository,
    IServerService serverService) : IInterlockingService
{
    public async Task<DataToInterlocking> SendData_Interlocking()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));
        var stations = await stationRepository.GetWhereIsStation();
        var stationIds = stations.Select(station => station.Id).ToList();
        var trackCircuits = await trackCircuitService.GetAllTrackCircuitDataList();
        var switchingDatas = await switchingMachineService.GetAllSwitchData();
        var levers = await leverService.GetAllLeverData();
        var directionSelfControlLevers = await directionSelfControlLeverService.GetAllWithState();
        var routeCentralControlLevers = await routeCentralControlLeverService.GetAllWithState();
        var directions = await directionRouteService.GetAllDirectionData();
        var destinationButtons = await destinationButtonService.GetAllButtonData();
        var timeOffset = await serverService.GetTimeOffsetAsync();

        // 各ランプの状態を取得
        var lamps = await GetLamps(stationIds, directionSelfControlLevers, routeCentralControlLevers);
        // 列番窓を取得
        var ttcWindows = await ttcStationControlService.GetTtcWindowsByStationIdsWithState(stationIds);

        var response = new DataToInterlocking
        {
            TrackCircuits = trackCircuits,

            Points = switchingDatas,

            // Todo: 方向てこのほうのリストを連結する
            PhysicalLevers = levers,

            PhysicalKeyLevers = directionSelfControlLevers
                .Select(DirectionSelfControlLeverService.ToKeyLeverData)
                .Concat(routeCentralControlLevers.Select(RouteCentralControlLeverService.ToKeyLeverData))
                .ToList(),

            PhysicalButtons = destinationButtons,

            Directions = directions,

            Retsubans = ttcWindows
                .Select(ToRetsubanData)
                .ToList(),

            // 各ランプの状態
            Lamps = lamps,

            TimeOffset = timeOffset
        };

        return response;
    }

    private async Task<Dictionary<string, bool>> GetLamps(
        List<string> stationIds,
        List<DirectionSelfControlLever> directionSelfControlLevers,
        List<RouteCentralControlLever> routeCentralControlLevers)
    {
        // Todo: 一旦仮でFalse
        var pwrFailure = stationIds.ToDictionary(
            stationId => $"{stationId}_PWR-FAILURE",
            _ => false);
        var ctcFailure = stationIds.ToDictionary(
            stationId => $"{stationId}_CTC-FAILURE",
            _ => false);
        var chrLamps = routeCentralControlLevers
            .SelectMany(RouteCentralControlLeverService.ToChrLamps)
            .ToDictionary();
        // 駅の時素状態を取得
        var stationTimerStates = (await stationRepository.GetTimerStatesByStationIds(stationIds))
            .ToDictionary(
                timerState => $"{timerState.StationId}_{timerState.Seconds}TEK",
                timerState => timerState.IsTimerConditionMet);

        return pwrFailure
            .Concat(ctcFailure)
            .Concat(chrLamps)
            .Concat(stationTimerStates)
            .ToDictionary();
    }

    /// <summary>
    /// レバーの物理状態を設定する
    /// </summary>
    public async Task<InterlockingLeverData> SetPhysicalLeverData(InterlockingLeverData leverData)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));
        return await leverService.SetPhysicalLeverData(leverData);
    }

    /// <summary>
    /// 鍵てこの物理状態を設定する
    /// </summary>
    /// <param name="keyLeverData"></param>
    /// <param name="memberId">DiscordのメンバーID</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">名前がどの鍵てこにも一致しない場合</exception>
    public async Task<InterlockingKeyLeverData> SetPhysicalKeyLeverData(
        InterlockingKeyLeverData keyLeverData, ulong? memberId)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));
        // 開放てこの判定を先に入れる
        var directionResult = await directionSelfControlLeverService.TrySetKeyLeverData(keyLeverData, memberId);
        if (directionResult != null)
        {
            return directionResult;
        }

        // CTC切替てこの判定
        var routeCentralResult = await routeCentralControlLeverService.TrySetKeyLeverData(keyLeverData, memberId);
        if (routeCentralResult != null)
        {
            return routeCentralResult;
        }

        throw new ArgumentException("Invalid key lever name");
    }

    /// <summary>
    /// 着点ボタンの物理状態を設定する
    /// </summary>
    public async Task<DestinationButtonData> SetDestinationButtonState(DestinationButtonData buttonData)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));
        return await destinationButtonService.SetState(buttonData);
    }

    public async Task ResetRaisedButtonsAsync()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));
        await destinationButtonService.ResetRaisedButtonsAsync();
    }

    public static InterlockingRetsubanData ToRetsubanData(TtcWindow ttcWindow)
    {
        return new()
        {
            Name = ttcWindow.Name,
            Retsuban = ttcWindow.TtcWindowState?.TrainNumber ?? "",
        };
    }
}
