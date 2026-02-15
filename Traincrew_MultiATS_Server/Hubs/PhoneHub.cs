using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "PhonePolicy"
)]
public class PhoneHub(IPhoneService phoneService) : Hub<IPhoneClientContract>, IPhoneHubContract
{
    public async Task Login(string myNumber)
    {
        await phoneService.LoginAsync(Context.ConnectionId, myNumber);
    }

    public async Task Call(string targetNumber)
    {
        await phoneService.CallAsync(Context.ConnectionId, targetNumber);
    }

    public async Task Answer(string callerConnectionId)
    {
        await phoneService.AnswerAsync(Context.ConnectionId, callerConnectionId);
    }

    public async Task Reject(string callerConnectionId)
    {
        await phoneService.RejectAsync(Context.ConnectionId, callerConnectionId);
    }

    public async Task Hangup(string targetConnectionId)
    {
        await phoneService.HangupAsync(Context.ConnectionId, targetConnectionId);
    }

    public async Task Busy(string callerConnectionId)
    {
        await phoneService.BusyAsync(Context.ConnectionId, callerConnectionId);
    }

    public async Task Hold(string targetId)
    {
        await phoneService.HoldAsync(Context.ConnectionId, targetId);
    }

    public async Task Resume(string targetId)
    {
        await phoneService.ResumeAsync(Context.ConnectionId, targetId);
    }

    public override async Task OnDisconnectedAsync(System.Exception? exception)
    {
        await phoneService.OnDisconnectedAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
