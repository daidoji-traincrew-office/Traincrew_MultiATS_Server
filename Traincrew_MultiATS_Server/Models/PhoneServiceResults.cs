namespace Traincrew_MultiATS_Server.Models;

public record LoginResult(string ConnectionId);

public abstract record CallResult
{
    public record Incoming(string CallerNumber, string CallerConnectionId, List<string> MemberConnectionIds) : CallResult;
    public record TargetBusy(string ConnectionId) : CallResult;
    public record CallerNotRegistered() : CallResult;
}

public abstract record AnswerResult
{
    public record Answered(string CallerConnectionId, string AnswererConnectionId, List<string> OtherMemberConnectionIds) : AnswerResult;
    public record SessionNotFound(string ConnectionId) : AnswerResult;
}

public record SingleNotifyResult(string? TargetConnectionId, string FromConnectionId);
