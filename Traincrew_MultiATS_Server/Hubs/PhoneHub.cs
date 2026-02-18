using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Authentication;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "PhonePolicy"
)]
public class PhoneHub(
    IPhoneService phoneService,
    EnableAuthorizationStore enableAuthorizationStore
) : Hub<IPhoneClientContract>, IPhoneHubContract
{
    public async Task Login(string myNumber)
    {
        var userId = GetUserId(myNumber);
        await phoneService.LoginAsync(Context.ConnectionId, userId, myNumber);
    }

    public async Task<CallResponse> Call(string targetNumber)
    {
        var result = await phoneService.CallAsync(Context.ConnectionId, targetNumber);
        switch (result)
        {
            case CallResult.Incoming incoming:
                foreach (var id in incoming.MemberConnectionIds)
                {
                    await Clients.Client(id).ReceiveIncoming(incoming.CallerNumber);
                }
                return new CallResponse(true);
            default:
                return new CallResponse(false);
        }
    }

    public async Task Answer()
    {
        var result = await phoneService.AnswerAsync(Context.ConnectionId);
        switch (result)
        {
            case AnswerResult.Answered answered:
                await Clients.Client(answered.CallerConnectionId).ReceiveAnswered();
                foreach (var id in answered.OtherMemberConnectionIds)
                {
                    await Clients.Client(id).ReceiveCancel();
                }
                return;
            default:
                throw new HubException("No active incoming call found.");
        }
    }

    public async Task Reject()
    {
        var result = await phoneService.RejectAsync(Context.ConnectionId);
        if (result != null)
        {
            foreach (var id in result.TargetConnectionIds)
            {
                await Clients.Client(id).ReceiveReject();
            }
        }
    }

    public async Task Hangup()
    {
        var result = await phoneService.HangupAsync(Context.ConnectionId);
        if (result != null)
        {
            foreach (var id in result.TargetConnectionIds)
            {
                await Clients.Client(id).ReceiveHangup();
            }
        }
    }

    public async Task Hold()
    {
        var result = await phoneService.HoldAsync(Context.ConnectionId);
        if (result != null)
        {
            foreach (var id in result.TargetConnectionIds)
            {
                await Clients.Client(id).ReceiveHoldRequest();
            }
        }
    }

    public async Task Resume()
    {
        var result = await phoneService.ResumeAsync(Context.ConnectionId);
        if (result != null)
        {
            foreach (var id in result.TargetConnectionIds)
            {
                await Clients.Client(id).ReceiveResumeRequest();
            }
        }
    }

    private string GetUserId(string myNumber)
    {
        var enableAuthorization = enableAuthorizationStore.EnableAuthorization;
        var userId = Context.User?.FindFirst(Claims.Subject)?.Value;
        if (userId != null)
        {
            return userId;
        }

        if (enableAuthorization)
        {
            throw new HubException("User not authenticated");
        }

        // Authorizationが無効な場合は、userIdをmyNumberと同じとみなす（ローカル開発用）
        return myNumber;
    }

    public override async Task OnDisconnectedAsync(System.Exception? exception)
    {
        var result = await phoneService.OnDisconnectedAsync(Context.ConnectionId);
        if (result != null)
        {
            foreach (var id in result.TargetConnectionIds)
            {
                await Clients.Client(id).ReceiveHangup();
            }
        }
        await base.OnDisconnectedAsync(exception);
    }
}
