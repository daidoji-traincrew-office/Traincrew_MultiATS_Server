namespace Traincrew_MultiATS_Server.Common.Contract;

public interface IPhoneHubContract
{
    Task Login(string myNumber);
    Task Call(string targetNumber);
    Task Answer(string callerConnectionId);
    Task Reject(string callerConnectionId);
    Task Hangup(string targetConnectionId);
    Task Busy(string callerConnectionId);
    Task Hold(string targetId);
    Task Resume(string targetId);
}

public interface IPhoneClientContract
{
    Task ReceiveLoginSuccess(string myConnectionId);
    Task ReceiveIncoming(string fromNumber, string callerConnectionId);
    Task ReceiveAnswered(string responderId);
    Task ReceiveCancel(string callerConnectionId);
    Task ReceiveReject(string fromId);
    Task ReceiveBusy(string fromId);
    Task ReceiveHangup(string fromId);
    Task ReceiveHoldRequest(string fromId);
    Task ReceiveResumeRequest(string fromId);
}
