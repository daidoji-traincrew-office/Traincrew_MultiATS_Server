using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
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
