using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;

namespace Traincrew_MultiATS_Server.Hubs;

// Todo: 司令員操作可 
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class CommanderTableHub: Hub
{
    
}