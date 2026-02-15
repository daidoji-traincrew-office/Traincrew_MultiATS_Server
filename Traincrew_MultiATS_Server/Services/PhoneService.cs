using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Repositories.PhoneSession;

namespace Traincrew_MultiATS_Server.Services;

public interface IPhoneService
{
    Task LoginAsync(string connectionId, string myNumber);
    Task CallAsync(string connectionId, string targetNumber);
    Task AnswerAsync(string connectionId, string callerConnectionId);
    Task RejectAsync(string connectionId, string callerConnectionId);
    Task HangupAsync(string connectionId, string targetConnectionId);
    Task BusyAsync(string connectionId, string callerConnectionId);
    Task HoldAsync(string connectionId, string targetId);
    Task ResumeAsync(string connectionId, string targetId);
    Task OnDisconnectedAsync(string connectionId);
}

public class PhoneService(
    PhoneSessionStore sessionStore,
    IPhoneSessionRepository sessionRepository,
    IHubContext<PhoneHub, IPhoneClientContract> hubContext
) : IPhoneService
{
    public async Task LoginAsync(string connectionId, string myNumber)
    {
        sessionStore.Register(connectionId, myNumber);
        await hubContext.Clients.Client(connectionId).ReceiveLoginSuccess(connectionId);
    }

    public async Task CallAsync(string connectionId, string targetNumber)
    {
        var callerNumber = sessionStore.GetNumberByConnectionId(connectionId);
        if (callerNumber == null)
        {
            return;
        }

        // グループメンバー確認
        var members = sessionStore.GetMembersByNumber(targetNumber);
        if (members == null || members.Count == 0)
        {
            await hubContext.Clients.Client(connectionId).ReceiveBusy(connectionId);
            return;
        }

        // セッション生成
        var session = await sessionRepository.CreateSessionAsync(callerNumber, connectionId, targetNumber);

        // 着信通知（グループ全員に）
        foreach (var memberConnectionId in members)
        {
            await hubContext.Clients.Client(memberConnectionId).ReceiveIncoming(callerNumber, connectionId);
        }
    }

    public async Task AnswerAsync(string connectionId, string callerConnectionId)
    {
        // セッション取得
        var session = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(callerConnectionId);
        if (session == null)
        {
            await hubContext.Clients.Client(connectionId).ReceiveHangup(callerConnectionId);
            return;
        }

        // セッション更新（answered + target_connection_id設定）
        await sessionRepository.SetAnsweredAsync(session.Id, connectionId);

        // 発信側に応答通知
        await hubContext.Clients.Client(callerConnectionId).ReceiveAnswered(connectionId);

        // 他のグループメンバーにキャンセル通知
        var targetNumber = sessionStore.GetNumberByConnectionId(connectionId);
        if (targetNumber != null)
        {
            var members = sessionStore.GetMembersByNumber(targetNumber);
            if (members != null)
            {
                foreach (var memberConnectionId in members)
                {
                    if (memberConnectionId != connectionId)
                    {
                        await hubContext.Clients.Client(memberConnectionId).ReceiveCancel(callerConnectionId);
                    }
                }
            }
        }
    }

    public async Task RejectAsync(string connectionId, string callerConnectionId)
    {
        var session = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(callerConnectionId);
        if (session == null)
        {
            return;
        }

        // 全員拒否かどうかチェック（簡略化: 1人でも拒否したらreject扱い）
        await sessionRepository.UpdateStatusAsync(session.Id, PhoneCallStatus.Rejected);
        await hubContext.Clients.Client(callerConnectionId).ReceiveReject(connectionId);
    }

    public async Task HangupAsync(string connectionId, string targetConnectionId)
    {
        // 発信側として切断する場合
        var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
        if (sessionAsCaller != null)
        {
            await sessionRepository.EndSessionAsync(sessionAsCaller.Id);
            if (sessionAsCaller.TargetConnectionId != null)
            {
                await hubContext.Clients.Client(sessionAsCaller.TargetConnectionId).ReceiveHangup(connectionId);
            }
            return;
        }

        // 着信側として切断する場合
        var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
        if (sessionAsTarget != null)
        {
            await sessionRepository.EndSessionAsync(sessionAsTarget.Id);
            await hubContext.Clients.Client(sessionAsTarget.CallerConnectionId).ReceiveHangup(connectionId);
        }
    }

    public async Task BusyAsync(string connectionId, string callerConnectionId)
    {
        var session = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(callerConnectionId);
        if (session == null)
        {
            return;
        }

        // 全員ビジャーかどうかチェック（簡略化: 1人でもビジャーならbusy扱い）
        await sessionRepository.UpdateStatusAsync(session.Id, PhoneCallStatus.Busy);
        await hubContext.Clients.Client(callerConnectionId).ReceiveBusy(connectionId);
    }

    public async Task HoldAsync(string connectionId, string targetId)
    {
        // 発信側としてホールド
        var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
        if (sessionAsCaller != null && sessionAsCaller.TargetConnectionId == targetId)
        {
            await sessionRepository.UpdateStatusAsync(sessionAsCaller.Id, PhoneCallStatus.Held);
            await hubContext.Clients.Client(targetId).ReceiveHoldRequest(connectionId);
            return;
        }

        // 着信側としてホールド
        var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
        if (sessionAsTarget != null && sessionAsTarget.CallerConnectionId == targetId)
        {
            await sessionRepository.UpdateStatusAsync(sessionAsTarget.Id, PhoneCallStatus.Held);
            await hubContext.Clients.Client(targetId).ReceiveHoldRequest(connectionId);
        }
    }

    public async Task ResumeAsync(string connectionId, string targetId)
    {
        // 発信側として再開
        var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
        if (sessionAsCaller != null && sessionAsCaller.TargetConnectionId == targetId)
        {
            await sessionRepository.UpdateStatusAsync(sessionAsCaller.Id, PhoneCallStatus.Answered);
            await hubContext.Clients.Client(targetId).ReceiveResumeRequest(connectionId);
            return;
        }

        // 着信側として再開
        var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
        if (sessionAsTarget != null && sessionAsTarget.CallerConnectionId == targetId)
        {
            await sessionRepository.UpdateStatusAsync(sessionAsTarget.Id, PhoneCallStatus.Answered);
            await hubContext.Clients.Client(targetId).ReceiveResumeRequest(connectionId);
        }
    }

    public async Task OnDisconnectedAsync(string connectionId)
    {
        // PhoneSessionStore から削除
        sessionStore.Unregister(connectionId);

        // アクティブセッションがあれば Hangup と同等の処理
        var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
        if (sessionAsCaller != null)
        {
            await sessionRepository.EndSessionAsync(sessionAsCaller.Id);
            if (sessionAsCaller.TargetConnectionId != null)
            {
                await hubContext.Clients.Client(sessionAsCaller.TargetConnectionId).ReceiveHangup(connectionId);
            }
            return;
        }

        var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
        if (sessionAsTarget != null)
        {
            await sessionRepository.EndSessionAsync(sessionAsTarget.Id);
            await hubContext.Clients.Client(sessionAsTarget.CallerConnectionId).ReceiveHangup(connectionId);
        }
    }
}
