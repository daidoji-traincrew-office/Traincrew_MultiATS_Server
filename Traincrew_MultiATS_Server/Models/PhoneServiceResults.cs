namespace Traincrew_MultiATS_Server.Models;

public abstract record CallResult
{
    public record Incoming(string CallerNumber, string CallerConnectionId, List<string> MemberConnectionIds) : CallResult;
    public record TargetBusy() : CallResult;
    public record CallerNotRegistered() : CallResult;
}

public abstract record AnswerResult
{
    public record Answered(
        string CallerConnectionId, string AnswererConnectionId,
        string? CallerUserId, string? AnswererUserId,
        List<string> OtherMemberConnectionIds) : AnswerResult;
    public record SessionNotFound() : AnswerResult;
}

/// <summary>
/// 相手への通知情報。TargetConnectionIdsは同じ電話番号に所属する全メンバーのConnectionId。
/// </summary>
public record SingleNotifyResult(List<string> TargetConnectionIds);
