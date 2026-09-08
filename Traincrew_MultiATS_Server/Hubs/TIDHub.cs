using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Common.Contract;

namespace Traincrew_MultiATS_Server.Hubs;
// 司令員、乗務助役使用可
[Authorize(
	AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
	Policy = "TIDPolicy"
)]
public class TIDHub : Hub<ITIDClientContract>, ITIDHubContract
{
}
