using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.Station;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// CTCP装置卓
/// </summary>
public class CTCPService(
    IDateTimeRepository dateTimeRepository,
    DiscordService discordService,
    IInterlockingObjectRepository interlockingObjectRepository,
    IDestinationButtonRepository destinationButtonRepository,
    IGeneralRepository generalRepository,
    IStationRepository stationRepository,
    ILeverRepository leverRepository,
    IDirectionSelfControlLeverRepository directionSelfControlLeverRepository,
    TrackCircuitService trackCircuitService,
    TtcStationControlService ttcStationControlService,
    RouteService routeService,
    SwitchingMachineService switchingMachineService,
    DirectionRouteService directionRouteService,
    SignalService signalService,
    IMutexRepository mutexRepository)
{

    public async Task<DataToCTCP> SendData_CTCP()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));
        var stations = await stationRepository.GetWhereIsStation();
        var stationIds = stations.Select(station => station.Id).ToList();
        var trackCircuits = await trackCircuitService.GetAllTrackCircuitDataList();
        var lever = await routeService.GetAllCTCLeverData();
        var directionSelfControlLevers = await directionSelfControlLeverRepository.GetAllWithState();
        var directions = await directionRouteService.GetAllDirectionData();

        // List<string> clientData.ActiveStationsListの駅IDから、指定された駅にある信号機名称をList<string>で返すやつ
        var signalNames = await signalService.GetSignalNamesByStationIds(stationIds);
        // それら全部の信号の現示計算
        var signalIndications = await signalService.CalcSignalIndication(signalNames);
        // 各ランプの状態を取得
        var lamps = await GetLamps(stationIds);
        // 列番窓を取得
        var ttcWindows = await ttcStationControlService.GetTtcWindowsByStationIdsWithState(stationIds);

        var response = new DataToCTCP
        {
            TrackCircuits = trackCircuits,

            // Todo: 方向てこのほうのリストを連結する
            CTCLevers = lever
                .ToList(),

            // 駅扱てこの実装と両方渡し
            PhysicalKeyLevers = directionSelfControlLevers
                .Select(ToKeyLeverData)
                .ToList(),

            Retsubans = ttcWindows
                .Select(ToRetsubanData)
                .ToList(),

            // 各ランプの状態 
            Lamps = lamps,

            Signals = signalIndications
                .Select(pair => SignalService.ToSignalData(pair.Key, pair.Value))
                .ToList()
        };

        return response;
    }

    public async Task<Dictionary<string, bool>> GetLamps(List<string> stationIds)
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

    /// <summary>
    /// レバーの物理状態を設定する
    /// </summary>
    /// <param name="leverData"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<InterlockingLeverData> SetPhysicalLeverData(InterlockingLeverData leverData)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));
        var lever = await leverRepository.GetLeverByNameWithState(leverData.Name);
        if (lever == null)
        {
            throw new ArgumentException("Invalid lever name");
        }

        lever.LeverState.IsReversed = leverData.State;
        await generalRepository.Save(lever);
        return new()
        {
            Name = lever.Name,
            State = lever.LeverState.IsReversed
        };
    }

    public static InterlockingKeyLeverData ToKeyLeverData(DirectionSelfControlLever lever)
    {
        if (lever.DirectionSelfControlLeverState == null)
        {
            throw new ArgumentException("Invalid lever state");
        }

        return new()
        {
            Name = lever.Name,
            State = lever.DirectionSelfControlLeverState.IsReversed == NR.Reversed ? LNR.Right : LNR.Normal,
            IsKeyInserted = lever.DirectionSelfControlLeverState.IsInsertedKey
        };
    }

    public async Task<List<DestinationButton>> GetDestinationButtonsByStationIds(List<string> stationNames)
    {
        return await destinationButtonRepository.GetButtonsByStationIds(stationNames);
    }

    public static DestinationButtonData ToDestinationButtonData(DestinationButtonState buttonState)
    {
        return new DestinationButtonData
        {
            Name = buttonState.Name,
            IsRaised = buttonState.IsRaised,
            OperatedAt = buttonState.OperatedAt
        };
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