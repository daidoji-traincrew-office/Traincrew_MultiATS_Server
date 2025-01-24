using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;
// 司令員、乗務助役使用可
[Authorize(
	AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
	Policy = "TIDPolicy"
)]
public class TIDHub(TrackCircuitService trackCircuitService,
    SwitchingMachineService switchingMachineService) : Hub
{
    public async Task<ConstantDataToTID> SendData_TID()
    {
        var trackCircuitDataList = await trackCircuitService.GetAllTrackCircuitDataList();
        var switchingMachineDataList = (await switchingMachineService.GetAllSwitchingMachines())
            .Select(SwitchingMachineService.ToSwitchData)
            .ToList();
        return new()
        {
            TrackCircuitDatas = trackCircuitDataList,
            SwitchDatas = switchingMachineDataList,
            DirectionDatas = [] 
        };
    }
}
