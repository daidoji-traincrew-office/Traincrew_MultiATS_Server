using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

// 信号係員操作可・司令主任鍵使用可 
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "InterlockingPolicy"
)]
public class InterlockingHub(
    TrackCircuitService trackCircuitService,
    SignalService signalService,
    StationService stationService,
    InterlockingService interlockingService) : Hub
{
    public async Task<DataToInterlocking> SendData_Interlocking(List<string> activeStationsList)
    {
        // Todo: めんどいし、Interlocking全取得してOfTypeと変換作って動くようにする    
        var interlockingObjects = await interlockingService.GetObjectsByStationIds(activeStationsList);
        var destinationButtons = await interlockingService.GetDestinationButtonsByStationIds(activeStationsList);
        // List<string> clientData.ActiveStationsListの駅IDから、指定された駅にある信号機名称をList<string>で返すやつ
        var signalNames = await signalService.GetSignalNamesByStationIds(activeStationsList);
        // それら全部の信号の現示計算
        var signalIndications = await signalService.CalcSignalIndication(signalNames);
        var lamps = await interlockingService.GetLamps(activeStationsList);

        var response = new DataToInterlocking
        {
            TrackCircuits = await trackCircuitService.GetAllTrackCircuitDataList(),

            Points = interlockingObjects
                .OfType<SwitchingMachine>()
                .Select(SwitchingMachineService.ToSwitchData)
                .ToList(),

            // Todo: List<InterlockingLeverData> PhysicalLeversを設定する
            PhysicalLevers = interlockingObjects
                .OfType<Lever>()
                .Select(InterlockingService.ToLeverData)
                .ToList(),

            // Todo: List<InterlockingKeyLeverData> PhysicalKeyLeversを設定する
            // Todo: 鍵てこの実装
            PhysicalKeyLevers = new List<InterlockingKeyLeverData>(),

            // Todo: List<DestinationButtonState> PhysicalButtonsを設定する
            PhysicalButtons = destinationButtons
                .Select(button => button.DestinationButtonState)
                .ToList(),

            // Todo: List<InterlockingDirectionData> Directionsを設定する
            // Todo: 方向てこ実装、方向てこにフィルターする
            Directions = new List<DirectionData>(),

            // Todo: List<InterlockingRetsubanData> Retsubansを設定する
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

    public async Task SetPhysicalLeverData(InterlockingLeverData leverData)
    {
        await interlockingService.SetPhysicalLeverData(leverData);
    }

    public async Task SetDestinationButtonState(DestinationButtonState buttonData)
    {
        await interlockingService.SetDestinationButtonState(buttonData);
    }
}
