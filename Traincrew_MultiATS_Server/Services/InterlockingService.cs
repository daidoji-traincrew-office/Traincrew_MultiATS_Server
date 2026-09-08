using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.RouteCentralControlLever;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 連動装置装置卓
/// </summary>
public interface IInterlockingService
{
    Task<DataToInterlocking> SendData_Interlocking();
    Task<DataToInterlocking> BuildInterlockingDataAsync(CommonReads commonReads);
    Task<InterlockingLeverData> SetPhysicalLeverData(InterlockingLeverData leverData);
    Task<InterlockingKeyLeverData> SetPhysicalKeyLeverData(InterlockingKeyLeverData keyLeverData, ulong? memberId);
    Task<DestinationButtonData> SetDestinationButtonState(DestinationButtonData buttonData);
    Task ResetRaisedButtonsAsync();
    Task<List<InterlockingObject>> GetInterlockingObjects();
    Task<List<InterlockingObject>> GetObjectsByStationIds(List<string> stationIds);
    Task<List<DestinationButton>> GetDestinationButtonsByStationIds(List<string> stationNames);
}

public class InterlockingService(
    IDateTimeRepository dateTimeRepository,
    IDiscordService discordService,
    IInterlockingObjectRepository interlockingObjectRepository,
    IDestinationButtonRepository destinationButtonRepository,
    IGeneralRepository generalRepository,
    ILeverRepository leverRepository,
    IDirectionSelfControlLeverRepository directionSelfControlLeverRepository,
    IRouteCentralControlLeverRepository routeCentralControlLeverRepository,
    ISignalService signalService,
    IMutexRepository mutexRepository,
    ICommonReadsBuilder commonReadsBuilder,
    ILogger<InterlockingService> logger) : IInterlockingService
{

    public async Task<DataToInterlocking> SendData_Interlocking()
    {
        // 読み取り経路ではmutexを取らない。整合性はRepeatableReadスナップショットが担保する。
        // (書き込み経路のmutexは従来どおり残している)
        var commonReads = await commonReadsBuilder.BuildAsync();
        return await BuildInterlockingDataAsync(commonReads);
    }

    /// <summary>
    /// <see cref="CommonReads"/> から連動盤配信用データを組み立てる。
    /// mutexもトランザクションも張らない(呼び出し元が既に管理している前提)。
    /// </summary>
    public async Task<DataToInterlocking> BuildInterlockingDataAsync(CommonReads commonReads)
    {
        var stationIds = commonReads.StationIds;
        var lever = await leverRepository.GetAllWithState();
        var directionSelfControlLevers = await directionSelfControlLeverRepository.GetAllWithState();
        var destinationButtons = await destinationButtonRepository.GetAllWithState();

        // 各ランプの状態を取得
        var lamps = GetLamps(stationIds, commonReads.RouteCentralControlLevers, commonReads.StationTimerStates);

        var response = new DataToInterlocking
        {
            TrackCircuits = commonReads.TrackCircuits,

            Points = commonReads.Switches,

            // Todo: 方向てこのほうのリストを連結する
            PhysicalLevers = lever
                .Select(ToLeverData)
                .ToList(),

            PhysicalKeyLevers = directionSelfControlLevers
                .Select(ToKeyLeverData)
                .Concat(commonReads.RouteCentralControlLevers.Select(ToKeyLeverDataFromRouteCentral))
                .ToList(),

            PhysicalButtons = destinationButtons
                .Select(button => ToDestinationButtonData(button.DestinationButtonState))
                .ToList(),

            Directions = commonReads.Directions,

            Retsubans = commonReads.TtcWindows
                .Select(ToRetsubanData)
                .ToList(),

            // 各ランプの状態
            Lamps = lamps,

            TimeOffset = commonReads.TimeOffset
        };

        return response;
    }

    private static Dictionary<string, bool> GetLamps(
        List<string> stationIds,
        List<RouteCentralControlLever> routeCentralControlLevers,
        List<StationTimerState> stationTimerStates)
    {
        // Todo: 一旦仮でFalse
        var pwrFailure = stationIds.ToDictionary(
            stationId => $"{stationId}_PWR-FAILURE",
            _ => false);
        var ctcFailure = stationIds.ToDictionary(
            stationId => $"{stationId}_CTC-FAILURE",
            _ => false);
        var chrLamps = routeCentralControlLevers
            .SelectMany(rccl =>
            {
                var stationId = rccl.StationId;
                return new KeyValuePair<string, bool>[]
                {
                    new(
                        $"{stationId}_CHRNK",
                        rccl.RouteCentralControlLeverState is { IsCenterControlled: false }),
                    new(
                        $"{stationId}_CHRRK",
                        rccl.RouteCentralControlLeverState is { IsCenterControlled: true })
                };
            })
            .ToDictionary();
        // 駅の時素状態を取得
        var stationTimerStateLamps = stationTimerStates
            .ToDictionary(
                timerState => $"{timerState.StationId}_{timerState.Seconds}TEK",
                timerState => timerState.IsTimerConditionMet);

        return pwrFailure
            .Concat(ctcFailure)
            .Concat(chrLamps)
            .Concat(stationTimerStateLamps)
            .ToDictionary();
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

        var oldState = lever.LeverState.IsReversed;
        lever.LeverState.IsReversed = leverData.State;

        if (oldState != leverData.State)
        {
            logger.LogDebug("[{LogType}] 名前: {LeverName} 状態: {OldState} -> {NewState}",
                "てこ操作", lever.Name, oldState, leverData.State);
        }

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
    public async Task<InterlockingKeyLeverData> SetPhysicalKeyLeverData(InterlockingKeyLeverData keyLeverData, ulong? memberId)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));
        // 開放てこの判定を先に入れる
        var directionkeyLever =
            await directionSelfControlLeverRepository.GetDirectionSelfControlLeverByNameWithState(keyLeverData.Name);
        if (directionkeyLever?.DirectionSelfControlLeverState != null)
        {
            return await SetDirectionSelfControlKeyLeverData(directionkeyLever, keyLeverData, memberId);
        }

        // CTC切替てこの判定
        var routeCentralControlLever =
            await routeCentralControlLeverRepository.GetByNameWithState(keyLeverData.Name);
        if (routeCentralControlLever?.RouteCentralControlLeverState != null)
        {
            return await SetRouteCentralControlKeyLeverData(routeCentralControlLever, keyLeverData, memberId);
        }

        throw new ArgumentException("Invalid key lever name");
    }

    private async Task<InterlockingKeyLeverData> SetDirectionSelfControlKeyLeverData(DirectionSelfControlLever directionkeyLever, InterlockingKeyLeverData keyLeverData, ulong? memberId)
    {

        // 更新後の値定義
        var isInsertedKey = directionkeyLever.DirectionSelfControlLeverState.IsInsertedKey;
        var isReversed = directionkeyLever.DirectionSelfControlLeverState.IsReversed;

        // 鍵を刺せるか確認
        var role = await discordService.GetRoleByMemberId(memberId);
        // 鍵を刺せるなら、鍵を処理する
        if (role.IsAdministrator)
        {
            isInsertedKey = keyLeverData.IsKeyInserted;
        }

        // 鍵が刺さっている場合、回す処理をする
        if (isInsertedKey)
        {
            isReversed = keyLeverData.State == LNR.Right ? NR.Reversed : NR.Normal;
        }

        // 変化があれば、更新する
        // ReSharper disable once InvertIf
        if (isInsertedKey != directionkeyLever.DirectionSelfControlLeverState.IsInsertedKey ||
            isReversed != directionkeyLever.DirectionSelfControlLeverState.IsReversed)
        {
            directionkeyLever.DirectionSelfControlLeverState.IsInsertedKey = isInsertedKey;
            directionkeyLever.DirectionSelfControlLeverState.IsReversed = isReversed;
            await generalRepository.Save(directionkeyLever);
        }

        return new()
        {
            Name = directionkeyLever.Name,
            State = isReversed == NR.Reversed ? LNR.Right : LNR.Normal,
            IsKeyInserted = isInsertedKey
        };
    }

    private async Task<InterlockingKeyLeverData> SetRouteCentralControlKeyLeverData(RouteCentralControlLever routeCentralControlLever, InterlockingKeyLeverData keyLeverData, ulong? memberId)
    {
        // 更新後の値定義
        var isInsertedKey = routeCentralControlLever.RouteCentralControlLeverState.IsInsertedKey;
        var isReversed = routeCentralControlLever.RouteCentralControlLeverState.IsReversed;

        // 鍵を刺せるか確認
        var role = await discordService.GetRoleByMemberId(memberId);
        // 鍵を刺せるなら、鍵を処理する
        if (role.IsAdministrator)
        {
            isInsertedKey = keyLeverData.IsKeyInserted;
        }

        // 鍵が刺さっている場合、回す処理をする
        if (isInsertedKey)
        {
            isReversed = keyLeverData.State == LNR.Right ? NR.Reversed : NR.Normal;
        }

        // 変化があれば、更新する
        // ReSharper disable once InvertIf
        if (isInsertedKey != routeCentralControlLever.RouteCentralControlLeverState.IsInsertedKey ||
            isReversed != routeCentralControlLever.RouteCentralControlLeverState.IsReversed)
        {
            routeCentralControlLever.RouteCentralControlLeverState.IsInsertedKey = isInsertedKey;
            routeCentralControlLever.RouteCentralControlLeverState.IsReversed = isReversed;
            // route_central_control_lever_stateは連動サーバーがis_center_controlledを書くため、列限定更新にすること
            await routeCentralControlLeverRepository.SetIsInsertedKeyAndIsReversedById(
                routeCentralControlLever.Id, isInsertedKey, isReversed);
        }

        return new()
        {
            Name = routeCentralControlLever.Name,
            State = isReversed == NR.Reversed ? LNR.Right : LNR.Left,
            IsKeyInserted = isInsertedKey
        };
    }

    /// <summary>
    /// 着点ボタンの物理状態を設定する
    /// </summary>
    /// <param name="buttonData"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<DestinationButtonData> SetDestinationButtonState(DestinationButtonData buttonData)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));
        var buttonObject = await destinationButtonRepository.GetButtonByName(buttonData.Name);
        if (buttonObject == null)
        {
            throw new ArgumentException("Invalid button name");
        }

        var oldIsRaised = buttonObject.DestinationButtonState.IsRaised;
        buttonObject.DestinationButtonState.OperatedAt = dateTimeRepository.GetNow();
        buttonObject.DestinationButtonState.IsRaised = buttonData.IsRaised;

        if (oldIsRaised != buttonData.IsRaised)
        {
            logger.LogDebug("[{LogType}] 名前: {ButtonName} 状態: {OldState} -> {NewState}",
                "着点操作", buttonObject.DestinationButtonState.Name, oldIsRaised, buttonData.IsRaised);
        }

        await generalRepository.Save(buttonObject.DestinationButtonState);
        return new()
        {
            Name = buttonObject.DestinationButtonState.Name,
            IsRaised = buttonObject.DestinationButtonState.IsRaised,
            OperatedAt = buttonObject.DestinationButtonState.OperatedAt
        };
    }

    public async Task ResetRaisedButtonsAsync()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));
        var now = dateTimeRepository.GetNow();
        await destinationButtonRepository.UpdateRaisedButtonsAsync(now);
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

    public static InterlockingKeyLeverData ToKeyLeverDataFromRouteCentral(RouteCentralControlLever lever)
    {
        if (lever.RouteCentralControlLeverState == null)
        {
            throw new ArgumentException("Invalid lever state");
        }

        return new()
        {
            Name = lever.Name,
            State = lever.RouteCentralControlLeverState.IsReversed == NR.Reversed ? LNR.Right : LNR.Left,
            IsKeyInserted = lever.RouteCentralControlLeverState.IsInsertedKey
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