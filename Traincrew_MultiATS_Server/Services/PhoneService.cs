using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.PhoneSession;

namespace Traincrew_MultiATS_Server.Services;

public interface IPhoneService
{
    Task<LoginResult> LoginAsync(string connectionId, string myNumber);
    Task<CallResult> CallAsync(string connectionId, string targetNumber);
    Task<AnswerResult> AnswerAsync(string connectionId, string callerConnectionId);
    Task<SingleNotifyResult?> RejectAsync(string connectionId, string callerConnectionId);
    Task<SingleNotifyResult?> HangupAsync(string connectionId, string targetConnectionId);
    Task<SingleNotifyResult?> BusyAsync(string connectionId, string callerConnectionId);
    Task<SingleNotifyResult?> HoldAsync(string connectionId, string targetId);
    Task<SingleNotifyResult?> ResumeAsync(string connectionId, string targetId);
    Task<SingleNotifyResult?> OnDisconnectedAsync(string connectionId);
}

public class PhoneService(
    PhoneSessionStore sessionStore,
    IPhoneSessionRepository sessionRepository,
    IDateTimeRepository dateTimeRepository
) : IPhoneService
{
    public Task<LoginResult> LoginAsync(string connectionId, string myNumber)
    {
        sessionStore.Register(connectionId, myNumber);
        return Task.FromResult(new LoginResult(connectionId));
    }

    public async Task<CallResult> CallAsync(string connectionId, string targetNumber)
    {
        var callerNumber = sessionStore.GetNumberByConnectionId(connectionId);
        if (callerNumber == null)
        {
            return new CallResult.CallerNotRegistered();
        }

        var members = sessionStore.GetMembersByNumber(targetNumber);
        if (members == null || members.Count == 0)
        {
            return new CallResult.TargetBusy(connectionId);
        }

        var now = dateTimeRepository.GetNow();
        await sessionRepository.CreateSessionAsync(callerNumber, connectionId, targetNumber, now);

        return new CallResult.Incoming(callerNumber, connectionId, members.ToList());
    }

    public async Task<AnswerResult> AnswerAsync(string connectionId, string callerConnectionId)
    {
        var session = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(callerConnectionId);
        if (session == null)
        {
            return new AnswerResult.SessionNotFound(connectionId);
        }

        await sessionRepository.SetAnsweredAsync(session.Id, connectionId);

        var otherMembers = new List<string>();
        var targetNumber = sessionStore.GetNumberByConnectionId(connectionId);
        if (targetNumber != null)
        {
            var members = sessionStore.GetMembersByNumber(targetNumber);
            if (members != null)
            {
                otherMembers = members.Where(id => id != connectionId).ToList();
            }
        }

        return new AnswerResult.Answered(callerConnectionId, connectionId, otherMembers);
    }

    public async Task<SingleNotifyResult?> RejectAsync(string connectionId, string callerConnectionId)
    {
        var session = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(callerConnectionId);
        if (session == null)
        {
            return null;
        }

        await sessionRepository.UpdateStatusAsync(session.Id, PhoneCallStatus.Rejected);
        return new SingleNotifyResult(callerConnectionId, connectionId);
    }

    public async Task<SingleNotifyResult?> HangupAsync(string connectionId, string targetConnectionId)
    {
        var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
        if (sessionAsCaller != null)
        {
            await sessionRepository.EndSessionAsync(sessionAsCaller.Id);
            return sessionAsCaller.TargetConnectionId != null
                ? new SingleNotifyResult(sessionAsCaller.TargetConnectionId, connectionId)
                : null;
        }

        var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
        if (sessionAsTarget != null)
        {
            await sessionRepository.EndSessionAsync(sessionAsTarget.Id);
            return new SingleNotifyResult(sessionAsTarget.CallerConnectionId, connectionId);
        }

        return null;
    }

    public async Task<SingleNotifyResult?> BusyAsync(string connectionId, string callerConnectionId)
    {
        var session = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(callerConnectionId);
        if (session == null)
        {
            return null;
        }

        await sessionRepository.UpdateStatusAsync(session.Id, PhoneCallStatus.Busy);
        return new SingleNotifyResult(callerConnectionId, connectionId);
    }

    public async Task<SingleNotifyResult?> HoldAsync(string connectionId, string targetId)
    {
        var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
        if (sessionAsCaller != null && sessionAsCaller.TargetConnectionId == targetId)
        {
            await sessionRepository.UpdateStatusAsync(sessionAsCaller.Id, PhoneCallStatus.Held);
            return new SingleNotifyResult(targetId, connectionId);
        }

        var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
        if (sessionAsTarget != null && sessionAsTarget.CallerConnectionId == targetId)
        {
            await sessionRepository.UpdateStatusAsync(sessionAsTarget.Id, PhoneCallStatus.Held);
            return new SingleNotifyResult(targetId, connectionId);
        }

        return null;
    }

    public async Task<SingleNotifyResult?> ResumeAsync(string connectionId, string targetId)
    {
        var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
        if (sessionAsCaller != null && sessionAsCaller.TargetConnectionId == targetId)
        {
            await sessionRepository.UpdateStatusAsync(sessionAsCaller.Id, PhoneCallStatus.Answered);
            return new SingleNotifyResult(targetId, connectionId);
        }

        var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
        if (sessionAsTarget != null && sessionAsTarget.CallerConnectionId == targetId)
        {
            await sessionRepository.UpdateStatusAsync(sessionAsTarget.Id, PhoneCallStatus.Answered);
            return new SingleNotifyResult(targetId, connectionId);
        }

        return null;
    }

    public async Task<SingleNotifyResult?> OnDisconnectedAsync(string connectionId)
    {
        sessionStore.Unregister(connectionId);

        var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
        if (sessionAsCaller != null)
        {
            await sessionRepository.EndSessionAsync(sessionAsCaller.Id);
            return sessionAsCaller.TargetConnectionId != null
                ? new SingleNotifyResult(sessionAsCaller.TargetConnectionId, connectionId)
                : null;
        }

        var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
        if (sessionAsTarget != null)
        {
            await sessionRepository.EndSessionAsync(sessionAsTarget.Id);
            return new SingleNotifyResult(sessionAsTarget.CallerConnectionId, connectionId);
        }

        return null;
    }
}
