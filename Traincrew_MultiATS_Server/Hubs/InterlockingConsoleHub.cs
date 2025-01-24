using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;

namespace Traincrew_MultiATS_Server.Hubs;

// Todo: 信号係員操作可・司令主任鍵使用可 
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class InterlockingConsoleHub: Hub
{
    
}