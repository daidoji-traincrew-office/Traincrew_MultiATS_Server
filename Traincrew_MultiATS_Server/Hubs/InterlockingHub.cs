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

        // Todo: TraincrewRole Authentications‚ğİ’è‚·‚é
        // response.Authentications =                       

        // Todo: List<InterlockingSwitchData> Points‚ğİ’è‚·‚é
        // response.Points =                              

        // Todo: List<InterlockingSignalData> Signals‚ğİ’è‚·‚é
        // response.Signals =                               

        // Todo: List<InterlockingLeverData> PhysicalLevers‚ğİ’è‚·‚é
        // response.PhysicalLevers =                           

        // Todo: List<DestinationButtonState> PhysicalButtons‚ğİ’è‚·‚é
        // response.PhysicalButtons =                        

        // Todo: List<InterlockingDirectionData> Directions‚ğİ’è‚·‚é
        // response.PhysicalButtons =                          

        // Todo: List<InterlockingRetsubanData> Retsubans‚ğİ’è‚·‚é
        // response.Retsubans =                              

        // Todo: List<Dictionary<string, bool>> Lamps‚ğİ’è‚·‚é
        // response.Lamps = 
        return response;
    }
}