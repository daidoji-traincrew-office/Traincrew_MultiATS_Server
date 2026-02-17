using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Models;
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
        var result = await phoneService.LoginAsync(Context.ConnectionId, myNumber);
        await Clients.Client(result.ConnectionId).ReceiveLoginSuccess(result.ConnectionId);
    }

    public async Task Call(string targetNumber)
    {
        var result = await phoneService.CallAsync(Context.ConnectionId, targetNumber);
        switch (result)
        {
            case CallResult.Incoming incoming:
                foreach (var id in incoming.MemberConnectionIds)
                {
                    await Clients.Client(id).ReceiveIncoming(incoming.CallerNumber, incoming.CallerConnectionId);
                }
                break;
            case CallResult.TargetBusy busy:
                await Clients.Client(busy.ConnectionId).ReceiveBusy(busy.ConnectionId);
                break;
        }
    }

    public async Task Answer(string callerConnectionId)
    {
        var result = await phoneService.AnswerAsync(Context.ConnectionId, callerConnectionId);
        switch (result)
        {
            case AnswerResult.Answered answered:
                await Clients.Client(answered.CallerConnectionId).ReceiveAnswered(answered.AnswererConnectionId);
                foreach (var id in answered.OtherMemberConnectionIds)
                {
                    await Clients.Client(id).ReceiveCancel(answered.CallerConnectionId);
                }
                break;
            case AnswerResult.SessionNotFound notFound:
                await Clients.Client(notFound.ConnectionId).ReceiveHangup(callerConnectionId);
                break;
        }
    }

    public async Task Reject(string callerConnectionId)
    {
        var result = await phoneService.RejectAsync(Context.ConnectionId, callerConnectionId);
        if (result?.TargetConnectionId != null)
        {
            await Clients.Client(result.TargetConnectionId).ReceiveReject(result.FromConnectionId);
        }
    }

    public async Task Hangup(string targetConnectionId)
    {
        var result = await phoneService.HangupAsync(Context.ConnectionId, targetConnectionId);
        if (result?.TargetConnectionId != null)
        {
            await Clients.Client(result.TargetConnectionId).ReceiveHangup(result.FromConnectionId);
        }
    }

    public async Task Busy(string callerConnectionId)
    {
        var result = await phoneService.BusyAsync(Context.ConnectionId, callerConnectionId);
        if (result?.TargetConnectionId != null)
        {
            await Clients.Client(result.TargetConnectionId).ReceiveBusy(result.FromConnectionId);
        }
    }

    public async Task Hold(string targetId)
    {
        var result = await phoneService.HoldAsync(Context.ConnectionId, targetId);
        if (result?.TargetConnectionId != null)
        {
            await Clients.Client(result.TargetConnectionId).ReceiveHoldRequest(result.FromConnectionId);
        }
    }

    public async Task Resume(string targetId)
    {
        var result = await phoneService.ResumeAsync(Context.ConnectionId, targetId);
        if (result?.TargetConnectionId != null)
        {
            await Clients.Client(result.TargetConnectionId).ReceiveResumeRequest(result.FromConnectionId);
        }
    }

    public override async Task OnDisconnectedAsync(System.Exception? exception)
    {
        var result = await phoneService.OnDisconnectedAsync(Context.ConnectionId);
        if (result?.TargetConnectionId != null)
        {
            await Clients.Client(result.TargetConnectionId).ReceiveHangup(result.FromConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
