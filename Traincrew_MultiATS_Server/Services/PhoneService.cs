using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.PhoneSession;

namespace Traincrew_MultiATS_Server.Services;

public interface IPhoneService
{
    Task LoginAsync(string connectionId, string myNumber);
    Task<CallResult> CallAsync(string connectionId, string targetNumber);
    Task<AnswerResult> AnswerAsync(string connectionId);
    Task<SingleNotifyResult?> RejectAsync(string connectionId);
    Task<SingleNotifyResult?> HangupAsync(string connectionId);
    Task<SingleNotifyResult?> HoldAsync(string connectionId);
    Task<SingleNotifyResult?> ResumeAsync(string connectionId);
    Task<SingleNotifyResult?> OnDisconnectedAsync(string connectionId);
}

public class PhoneService(
    PhoneSessionStore sessionStore,
    IPhoneSessionRepository sessionRepository,
    IDateTimeRepository dateTimeRepository
) : IPhoneService
{
    public Task LoginAsync(string connectionId, string myNumber)
    {
        sessionStore.Register(connectionId, myNumber);
        return Task.CompletedTask;
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
            return new CallResult.TargetBusy();
        }

        // 同じ番号からの重複着信防止: 既にこの発信元番号からの着信セッションがあればブロック
        var existingSession = await sessionRepository.GetActiveSessionByCallerNumberAndTargetNumberAsync(callerNumber, targetNumber);
        if (existingSession != null)
        {
            return new CallResult.TargetBusy();
        }

        // 相手が通話中（いずれかのメンバーが通話中セッションに参加中）かチェック
        var targetHasActiveSession = await sessionRepository.GetActiveSessionByTargetNumberAsync(targetNumber);
        if (targetHasActiveSession != null)
        {
            return new CallResult.TargetBusy();
        }

        var now = dateTimeRepository.GetNow();
        await sessionRepository.CreateSessionAsync(callerNumber, connectionId, targetNumber, now);

        return new CallResult.Incoming(callerNumber, connectionId, members.ToList());
    }

    public async Task<AnswerResult> AnswerAsync(string connectionId)
    {
        var targetNumber = sessionStore.GetNumberByConnectionId(connectionId);
        if (targetNumber == null)
        {
            return new AnswerResult.SessionNotFound();
        }

        var session = await sessionRepository.GetActiveSessionByTargetNumberAsync(targetNumber);
        if (session == null)
        {
            return new AnswerResult.SessionNotFound();
        }

        await sessionRepository.SetAnsweredAsync(session.Id, connectionId);

        var otherMembers = new List<string>();
        var members = sessionStore.GetMembersByNumber(targetNumber);
        if (members != null)
        {
            otherMembers = members.Where(id => id != connectionId).ToList();
        }

        return new AnswerResult.Answered(session.CallerConnectionId, connectionId, otherMembers);
    }

    public async Task<SingleNotifyResult?> RejectAsync(string connectionId)
    {
        var targetNumber = sessionStore.GetNumberByConnectionId(connectionId);
        if (targetNumber == null)
        {
            return null;
        }

        var session = await sessionRepository.GetActiveSessionByTargetNumberAsync(targetNumber);
        if (session == null)
        {
            return null;
        }

        await sessionRepository.UpdateStatusAsync(session.Id, PhoneCallStatus.Rejected);
        return new SingleNotifyResult([session.CallerConnectionId]);
    }

    public async Task<SingleNotifyResult?> HangupAsync(string connectionId)
    {
        var now = dateTimeRepository.GetNow();
        var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
        if (sessionAsCaller != null)
        {
            await sessionRepository.EndSessionAsync(sessionAsCaller.Id, now);
            if (sessionAsCaller.TargetConnectionId != null)
            {
                return new SingleNotifyResult([sessionAsCaller.TargetConnectionId]);
            }
            // 着信中（未応答）の場合、全メンバーにキャンセル通知
            var members = sessionStore.GetMembersByNumber(sessionAsCaller.TargetNumber);
            return members != null ? new SingleNotifyResult(members.ToList()) : null;
        }

        var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
        if (sessionAsTarget != null)
        {
            await sessionRepository.EndSessionAsync(sessionAsTarget.Id, now);
            return new SingleNotifyResult([sessionAsTarget.CallerConnectionId]);
        }

        return null;
    }

    public async Task<SingleNotifyResult?> HoldAsync(string connectionId)
    {
        var session = await FindActiveSessionForConnectionAsync(connectionId);
        if (session == null)
        {
            return null;
        }

        await sessionRepository.UpdateStatusAsync(session.Id, PhoneCallStatus.Held);
        var targetConnectionId = session.CallerConnectionId == connectionId
            ? session.TargetConnectionId
            : session.CallerConnectionId;
        return targetConnectionId != null ? new SingleNotifyResult([targetConnectionId]) : null;
    }

    public async Task<SingleNotifyResult?> ResumeAsync(string connectionId)
    {
        var session = await FindActiveSessionForConnectionAsync(connectionId);
        if (session == null)
        {
            return null;
        }

        await sessionRepository.UpdateStatusAsync(session.Id, PhoneCallStatus.Answered);
        var targetConnectionId = session.CallerConnectionId == connectionId
            ? session.TargetConnectionId
            : session.CallerConnectionId;
        return targetConnectionId != null ? new SingleNotifyResult([targetConnectionId]) : null;
    }

    public async Task<SingleNotifyResult?> OnDisconnectedAsync(string connectionId)
    {
        sessionStore.Unregister(connectionId);

        var now = dateTimeRepository.GetNow();
        var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
        if (sessionAsCaller != null)
        {
            await sessionRepository.EndSessionAsync(sessionAsCaller.Id, now);
            if (sessionAsCaller.TargetConnectionId != null)
            {
                return new SingleNotifyResult([sessionAsCaller.TargetConnectionId]);
            }
            var members = sessionStore.GetMembersByNumber(sessionAsCaller.TargetNumber);
            return members != null ? new SingleNotifyResult(members.ToList()) : null;
        }

        var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
        if (sessionAsTarget != null)
        {
            await sessionRepository.EndSessionAsync(sessionAsTarget.Id, now);
            return new SingleNotifyResult([sessionAsTarget.CallerConnectionId]);
        }

        return null;
    }

    private async Task<PhoneCallSession?> FindActiveSessionForConnectionAsync(string connectionId)
    {
        var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
        if (sessionAsCaller != null)
        {
            return sessionAsCaller;
        }

        return await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
    }
}
