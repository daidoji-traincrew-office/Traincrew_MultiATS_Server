using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

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
    InterlockingService interlockingService) : Hub<IInterlockingClientContract>, IInterlockingHubContract
{
    public async Task<DataToInterlocking> SendData_Interlocking(List<string> activeStationsList)
    {
        // Todo: クライアントごとに取得処理をするのではなく、サーバーで定時で取得処理をして、必要なクライアントに配る形式にする
        // Todo: めんどいし、Interlocking全取得してOfTypeと変換作って動くようにする    
        var allInterlockingObjects = await interlockingService.GetInterlockingObjects();
        var destinationButtons = await interlockingService.GetDestinationButtonsByStationIds(activeStationsList);
        // List<string> clientData.ActiveStationsListの駅IDから、指定された駅にある信号機名称をList<string>で返すやつ
        var signalNames = await signalService.GetSignalNamesByStationIds(activeStationsList);
        // それら全部の信号の現示計算
        var signalIndications = await signalService.CalcSignalIndication(signalNames);
        var lamps = await interlockingService.GetLamps(activeStationsList);

        var response = new DataToInterlocking
        {
            TrackCircuits = allInterlockingObjects
                .OfType<TrackCircuit>()
                .Select(TrackCircuitService.ToTrackCircuitData)
                .ToList(),

            Points = allInterlockingObjects
                .OfType<SwitchingMachine>()
                .Select(SwitchingMachineService.ToSwitchData)
                .ToList(),

            // Todo: 方向てこのほうのリストを連結する
            PhysicalLevers = allInterlockingObjects
                .OfType<Lever>()
                .Select(InterlockingService.ToLeverData)
                .ToList(),

            // Todo: 駅扱てこの実装と両方渡し
            PhysicalKeyLevers = allInterlockingObjects
                .OfType<DirectionSelfControlLever>()
                .Select(InterlockingService.ToKeyLeverData)
                .ToList(),

            PhysicalButtons = destinationButtons
                .Select(button => InterlockingService.ToDestinationButtonData(button.DestinationButtonState))
                .ToList(),

            Directions = allInterlockingObjects
                .OfType<DirectionRoute>()
                .Select(InterlockingService.ToDirectionData)
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

    public async Task SetPhysicalLeverData(InterlockingLeverData leverData)
    {
        await interlockingService.SetPhysicalLeverData(leverData);
    }

    public async Task<bool> SetPhysicalKeyLeverData(InterlockingKeyLeverData keyLeverData)
    {
        // MemberIDを取得
        var memberIdString = Context.User?.FindFirst(Claims.Subject)?.Value;
        ulong? memberId = memberIdString != null ? ulong.Parse(memberIdString) : null;
        return await interlockingService.SetPhysicalKeyLeverData(keyLeverData, memberId);
    }

    public async Task SetDestinationButtonState(DestinationButtonData buttonData)
    {
        await interlockingService.SetDestinationButtonState(buttonData);
    }
}
