using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.Station;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// 連動装置装置卓
/// </summary>
public class InterlockingService(
    IDateTimeRepository dateTimeRepository,
    IInterlockingObjectRepository interlockingObjectRepository,
    IDestinationButtonRepository destinationButtonRepository,
    IGeneralRepository generalRepository,
    IStationRepository stationRepository,
    ILeverRepository leverRepository)
{
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
    public async Task SetPhysicalLeverData(InterlockingLeverData leverData)
    {
        var lever = await leverRepository.GetLeverByNameWitState(leverData.Name);
        if (lever == null)
        {
            throw new ArgumentException("Invalid lever name");
        }

        lever.LeverState.IsReversed = leverData.State;
        await generalRepository.Save(lever);
    }

    /// <summary>
    /// 着点ボタンの物理状態を設定する
    /// </summary>
    /// <param name="buttonData"></param>
    /// <returns></returns>     
    /// <exception cref="ArgumentException"></exception>
    public async Task SetDestinationButtonState(DestinationButtonState buttonData)
    {
        var buttonObject = await destinationButtonRepository.GetButtonByName(buttonData.Name);
        if (buttonObject == null)
        {
            throw new ArgumentException("Invalid button name");
        }

        buttonObject.DestinationButtonState.OperatedAt = dateTimeRepository.GetNow();
        buttonObject.DestinationButtonState.IsRaised = buttonData.IsRaised;
        await generalRepository.Save(buttonObject.DestinationButtonState);
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

    public async Task<List<DestinationButton>> GetDestinationButtons()
    {
        var buttons = await destinationButtonRepository.GetAllButtons();
        return buttons.Values.ToList();
    }

    public async Task<List<DestinationButton>> GetDestinationButtonsByStationIds(List<string> stationNames)
    {
        return await destinationButtonRepository.GetButtonsByStationIds(stationNames);
    }

    public static DirectionData ToDirectionData(DirectionLever direction)
    {
        var state = LCR.Center;
        if (direction.DirectionLeverState.IsLRelayRaised == RaiseDrop.Raise)
        {
            state = LCR.Left;
        }
        else if (direction.DirectionLeverState.IsRRelayRaised == RaiseDrop.Raise)
        {
            state = LCR.Right;
        }
        return new()
        {
            Name = direction.Name,
            State = state
        };
    }
}