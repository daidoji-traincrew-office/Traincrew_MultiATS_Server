using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Services;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;


namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class InterlockingHub(TrackCircuitService trackCircuitService) : Hub
{
    public async Task<Models.DataToInterlocking> SendData_Interlocking(Models.DataToInterlocking dataToInterlocking)
    {
        Models.DataToInterlocking response = new Models.DataToInterlocking();
        response.TrackCircuits = await trackCircuitService.GetAllTrackCircuitDataList();

        // Todo: TraincrewRole Authentications��ݒ肷��
        // response.Authentications =                       

        // Todo: List<InterlockingSwitchData> Points��ݒ肷��
        // response.Points =                              

        // Todo: List<InterlockingSignalData> Signals��ݒ肷��
        // response.Signals =                               

        // Todo: List<InterlockingLeverData> PhysicalLevers��ݒ肷��
        // response.PhysicalLevers =                           

        // Todo: List<DestinationButtonState> PhysicalButtons��ݒ肷��
        // response.PhysicalButtons =                        

        // Todo: List<InterlockingDirectionData> Directions��ݒ肷��
        // response.PhysicalButtons =                          

        // Todo: List<InterlockingRetsubanData> Retsubans��ݒ肷��
        // response.Retsubans =                              

        // Todo: List<Dictionary<string, bool>> Lamps��ݒ肷��
        // response.Lamps = 
        return response;
    }
}