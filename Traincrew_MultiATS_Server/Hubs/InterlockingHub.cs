using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class InterlockingHub(
    TrackCircuitService trackCircuitService,
    SignalService signalService,
    StationService stationService) : Hub
{
    public async Task<DataToInterlocking> SendData_Interlocking(ConstantDataFromInterlocking clientData)
    {
        // Todo: めんどいし、Interlocking全取得してOfTypeと変換作って動くようにする
        List<InterlockingObject> interlockingObjects = [];
        List<DestinationButton> destinationButtons = [];
        // List<string> clientData.ActiveStationsListの駅IDから、指定された駅にある信号機名称をList<string>で返すやつ
        var stationNames = await stationService.GetStationNamesByIds(clientData.ActiveStationsList);
        var signalNames = await signalService.GetSignalNamesByStationNames(stationNames);
        // それら全部の信号の現示計算
        var signalIndications = await signalService.CalcSignalIndication(signalNames);

        var response = new DataToInterlocking
        {
            TrackCircuits = await trackCircuitService.GetAllTrackCircuitDataList(),
            // Todo: TraincrewRole Authenticationsを設定する(role認証がどうにかなったあたりでつなぎこむ)
            // Authentications =                       
            Points = interlockingObjects
                .OfType<SwitchingMachine>()
                .Select(SwitchingMachineService.ToInterlockingSwitchData)
                .ToList(),
            // Todo: List<InterlockingLeverData> PhysicalLeversを設定する
            // Todo: 変換実装と、方向てこ以外のてこにフィルターする
            PhysicalLevers = interlockingObjects
                .OfType<Lever>()
                .Select(lever => new InterlockingLeverData())
                .ToList(),
            // Todo: List<DestinationButtonState> PhysicalButtonsを設定する
            // Todo: こいつはまずレスポンス定義するところから
            // PhysicalButtons = [] 

            // Todo: List<InterlockingDirectionData> Directionsを設定する
            // Todo: 方向てこ実装、方向てこにフィルターする
            Directions = [],

            // Todo: List<InterlockingRetsubanData> Retsubansを設定する
            // Todo: 列番表示の実装から
            // Retsubans =                              

            // Todo: List<Dictionary<string, bool>> Lampsを設定する
            // Todo: これは何を設定すればええんや・・・？
            // response.Lamps = 
            Signals = signalIndications
                .Select(pair => SignalService.ToSignalData(pair.Key, pair.Value))
                .ToList()
        };
        return response;
    }
}