using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

// 信号係員操作可
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "InterlockingPolicy"
)]
public class CTCPHub(CTCPService ctcpService) : Hub<ICTCPClientContract>, ICTCPHubContract
{
    public async Task<DataToCTCP> SendData_CTCP()
    {
        return await ctcpService.SendData_CTCP();
    }

    public async Task<Common.Models.RouteData> SetCtcRelay(string TcName, RaiseDrop raiseDrop)
    {
        return await ctcpService.SetCtcRelay(TcName, raiseDrop);
    }
}