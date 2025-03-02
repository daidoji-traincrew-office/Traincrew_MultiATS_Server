using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class TIDHub(TrackCircuitService trackCircuitService,
    SwitchingMachineService switchingMachineService) : Hub
{
    public async Task<Models.ConstantDataToTID> SendData_TID()
    {
        List<TrackCircuitData> trackCircuitDataList = await trackCircuitService.GetAllTrackCircuitDataList();
        return new Models.ConstantDataToTID()
        {
            TrackCircuitDatas = trackCircuitDataList,
            SwitchDatas = (await switchingMachineService.GetAllSwitchingMachines())
                .Select(SwitchingMachineService.ToSwitchData).ToList(),
            DirectionDatas = new List<Models.DirectionData>()
        };
    }
}
