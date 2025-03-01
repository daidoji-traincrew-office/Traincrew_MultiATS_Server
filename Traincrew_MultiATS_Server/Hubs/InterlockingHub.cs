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

        // Todo: TraincrewRole Authentications��ݒ肷��
        // response.Authentications =                       

        // Todo: List<InterlockingSwitchData> Points��ݒ肷��
        // response.Points =                              

        // List<string> clientData.ActiveStationsList�̉wID����A�w�肳�ꂽ�w�ɂ���M���@���̂�List<string>�ŕԂ����
        var stationNames = await stationService.GetStationNamesByIds(clientData.ActiveStationsList);
        var signalNames = await signalService.GetSignalNamesByStationNames(stationNames);
        // �����S���̐M���̌����v�Z
        var signalIndications = await signalService.CalcSignalIndication(signalNames);
        response.Signals = signalIndications.Select(pair => new SignalData
        {
            Name = pair.Key,
            phase = pair.Value
        }).ToList();

        // Todo: List<InterlockingLeverData> PhysicalLevers��ݒ肷��
        // response.PhysicalLevers =                           

        // Todo: List<DestinationButtonState> PhysicalButtons��ݒ肷��
        // response.PhysicalButtons =                        

        // Todo: List<InterlockingDirectionData> Directions��ݒ肷��
        // response.Directions =                          

        // Todo: List<InterlockingRetsubanData> Retsubans��ݒ肷��
        // response.Retsubans =                              

        // Todo: List<Dictionary<string, bool>> Lamps��ݒ肷��
        // response.Lamps = 
        return response;
    }
}