using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

// 司令員操作可 
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "CommanderTablePolicy"
)]
public class CommanderTableHub(TrackCircuitService trackCircuitService) : Hub
{
    public async Task DeleteTrain(string trainName)
    {
        await trackCircuitService.ClearTrackCircuitByTrainNumber(trainName);
    }
}