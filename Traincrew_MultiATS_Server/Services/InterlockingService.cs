using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.DirectionRoute;
using Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.Station;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 連動装置装置卓
/// </summary>
public class InterlockingService(
    IDateTimeRepository dateTimeRepository,
    DiscordService discordService,
    IInterlockingObjectRepository interlockingObjectRepository,
    IDestinationButtonRepository destinationButtonRepository,
    IGeneralRepository generalRepository,
    IStationRepository stationRepository,
    ILeverRepository leverRepository,
    IDirectionRouteRepository directionRouteRepository,
    IDirectionSelfControlLeverRepository directionSelfControlLeverRepository,
    TrackCircuitService trackCircuitService,
    ISwitchingMachineRepository switchingMachineRepository,
    SignalService signalService)
{
    public async Task<DataToInterlocking> SendData_Interlocking()
    {
        var stations = await stationRepository.GetWhereIsStation();
        var stationIds = stations.Select(station => station.Id).ToList();
        var trackCircuits = await trackCircuitService.GetAllTrackCircuitDataList();
        var switchingMachine = await switchingMachineRepository.GetSwitchingMachinesWithState();
        var lever = await leverRepository.GetAllWithState();
        var directionSelfControlLevers = await directionSelfControlLeverRepository.GetAllWithState();
        var directionRoutes = await directionRouteRepository.GetAllWithState();
        var destinationButtons = await destinationButtonRepository.GetAllWithState();

        // List<string> clientData.ActiveStationsListの駅IDから、指定された駅にある信号機名称をList<string>で返すやつ
        var signalNames = await signalService.GetSignalNamesByStationIds(stationIds);
        // それら全部の信号の現示計算
        var signalIndications = await signalService.CalcSignalIndication(signalNames);
        // 各ランプの状態を取得
        var lamps = await GetLamps(stationIds);

        var response = new DataToInterlocking
        {
            TrackCircuits = trackCircuits,

            Points = switchingMachine
                .Select(SwitchingMachineService.ToSwitchData)
                .ToList(),

            // Todo: 方向てこのほうのリストを連結する
            PhysicalLevers = lever
                .Select(ToLeverData)
                .ToList(),

            // 駅扱てこの実装と両方渡し
            PhysicalKeyLevers = directionSelfControlLevers
                .Select(ToKeyLeverData)
                .ToList(),

            PhysicalButtons = destinationButtons
                .Select(button => ToDestinationButtonData(button.DestinationButtonState))
                .ToList(),

            Directions = directionRoutes
                .Select(ToDirectionData)
                .ToList(),

            // Todo: 列番表示の実装から
            Retsubans = new List<InterlockingRetsubanData>(),

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

    /// <summary>
    /// 鍵てこの物理状態を設定する
    /// </summary>
    /// <param name="keyLeverData"></param>
    /// <param name="memberId">DiscordのメンバーID</param>
    /// <returns></returns>
    internal async Task<bool> SetPhysicalKeyLeverData(InterlockingKeyLeverData keyLeverData, ulong? memberId)
    {
        // 駅扱の判定を先に入れる
        var directionkeyLever =
            await directionSelfControlLeverRepository.GetDirectionSelfControlLeverByNameWithState(keyLeverData.Name);
        if (directionkeyLever == null || directionkeyLever.DirectionSelfControlLeverState == null)
        {
            throw new ArgumentException("Invalid key lever name");
        }

        // 鍵を刺せるか確認
        var role = await discordService.GetRoleByMemberId(memberId);
        // 鍵を刺せないなら処理終了
        if (!role.IsAdministrator)
        {
            return false;
        }
        // 鍵てこを処理する
        directionkeyLever.DirectionSelfControlLeverState.IsInsertedKey = keyLeverData.IsKeyInserted;
        directionkeyLever.DirectionSelfControlLeverState.IsReversed =
            keyLeverData.State == LNR.Right ? NR.Reversed : NR.Normal;
        await generalRepository.Save(directionkeyLever);
        return true;
    }

    /// <summary>
    /// 着点ボタンの物理状態を設定する
    /// </summary>
    /// <param name="buttonData"></param>
    /// <returns></returns>     
    /// <exception cref="ArgumentException"></exception>
    public async Task<DestinationButtonData> SetDestinationButtonState(DestinationButtonData buttonData)
    {
        var buttonObject = await destinationButtonRepository.GetButtonByName(buttonData.Name);
        if (buttonObject == null)
        {
            throw new ArgumentException("Invalid button name");
        }

        buttonObject.DestinationButtonState.OperatedAt = dateTimeRepository.GetNow();
        buttonObject.DestinationButtonState.IsRaised = buttonData.IsRaised;
        await generalRepository.Save(buttonObject.DestinationButtonState);
        return new()
        {
            Name = buttonObject.DestinationButtonState.Name,
            IsRaised = buttonObject.DestinationButtonState.IsRaised,
            OperatedAt = buttonObject.DestinationButtonState.OperatedAt
        };
    }


    public async Task<List<InterlockingObject>> GetInterlockingObjects()
    {
        return await interlockingObjectRepository.GetAllWithState();
    }

    public async Task<List<InterlockingObject>> GetObjectsByStationIds(List<string> stationIds)
    {
        return await interlockingObjectRepository.GetObjectsByStationIdsWithState(stationIds);
    }

    public static InterlockingLeverData ToLeverData(Lever lever)
    {
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

    public static DirectionData ToDirectionData(DirectionRoute direction)
    {
        if (direction.DirectionRouteState == null)
        {
            throw new ArgumentException("Invalid direction state");
        }

        var state = LCR.Center;
        if (direction.DirectionRouteState.isLr == LR.Left)
        {
            state = LCR.Left;
        }
        else if (direction.DirectionRouteState.isLr == LR.Right)
        {
            state = LCR.Right;
        }

        return new()
        {
            Name = direction.Name,
            State = state
        };
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
}