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
        DataToInterlocking response = new DataToInterlocking();
        response.TrackCircuits = await trackCircuitService.GetAllTrackCircuitDataList();

        // Todo: TraincrewRole Authenticationsを設定する(role認証がどうにかなったあたりでつなぎこむ)
        // response.Authentications =                       

        // Todo: List<InterlockingSwitchData> Pointsを設定する
        // response.Points =                              

        // List<string> clientData.ActiveStationsListの駅IDから、指定された駅にある信号機名称をList<string>で返すやつ
        var stationNames = await stationService.GetStationNamesByIds(clientData.ActiveStationsList);
        var signalNames = await signalService.GetSignalNamesByStationNames(stationNames);
        // それら全部の信号の現示計算
        var signalIndications = await signalService.CalcSignalIndication(signalNames);
        response.Signals = signalIndications.Select(pair => new SignalData
        {
            Name = pair.Key,
            phase = pair.Value
        }).ToList();

        // Todo: List<InterlockingLeverData> PhysicalLeversを設定する
        // response.PhysicalLevers =                           

        // Todo: List<DestinationButtonState> PhysicalButtonsを設定する
        // response.PhysicalButtons =                        

        // Todo: List<InterlockingDirectionData> Directionsを設定する
        // response.PhysicalButtons =                          

        // Todo: List<InterlockingRetsubanData> Retsubansを設定する
        // response.Retsubans =                              

        // Todo: List<Dictionary<string, bool>> Lampsを設定する
        // response.Lamps = 
        return response;
    }
}