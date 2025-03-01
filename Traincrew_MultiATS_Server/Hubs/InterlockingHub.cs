using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Services;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;


namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class InterlockingHub(
    TrackCircuitService trackCircuitService,
    SignalService signalService,
    StationService stationService) : Hub
{
    public async Task<Models.DataToInterlocking> SendData_Interlocking(Models.ConstantDataFromInterlocking clientData)
    {
        Models.DataToInterlocking response = new Models.DataToInterlocking();
        response.TrackCircuits = await trackCircuitService.GetAllTrackCircuitDataList();

        // Todo: TraincrewRole Authenticationsを設定する
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
        // response.Directions =                          

        // Todo: List<InterlockingRetsubanData> Retsubansを設定する
        // response.Retsubans =                              

        // Todo: List<Dictionary<string, bool>> Lampsを設定する
        // response.Lamps = 
        return response;
    }
}