namespace Traincrew_MultiATS_Server.Common.Contract;

/// <summary>
/// 電話HubのサーバーメソッドContract。クライアントから呼び出される。
/// </summary>
public interface IPhoneHubContract
{
    /// <summary>
    /// 電話番号を指定してログインする。
    /// </summary>
    /// <param name="myNumber">自局の電話番号</param>
    Task Login(string myNumber);

    /// <summary>
    /// 指定した電話番号に発信する。
    /// </summary>
    /// <param name="targetNumber">発信先の電話番号</param>
    Task Call(string targetNumber);

    /// <summary>
    /// 着信に応答する。
    /// </summary>
    /// <param name="callerConnectionId">発信者のConnectionId</param>
    Task Answer(string callerConnectionId);

    /// <summary>
    /// 着信を拒否する。
    /// </summary>
    /// <param name="callerConnectionId">発信者のConnectionId</param>
    Task Reject(string callerConnectionId);

    /// <summary>
    /// 通話を切断する。
    /// </summary>
    /// <param name="targetConnectionId">切断対象のConnectionId</param>
    Task Hangup(string targetConnectionId);

    /// <summary>
    /// 話中応答を返す。
    /// </summary>
    /// <param name="callerConnectionId">発信者のConnectionId</param>
    Task Busy(string callerConnectionId);

    /// <summary>
    /// 通話を保留にする。
    /// </summary>
    /// <param name="targetId">保留対象のConnectionId</param>
    Task Hold(string targetId);

    /// <summary>
    /// 保留中の通話を再開する。
    /// </summary>
    /// <param name="targetId">再開対象のConnectionId</param>
    Task Resume(string targetId);
}

/// <summary>
/// 電話Hubのクライアントコールバック Contract。サーバーからクライアントへ通知される。
/// </summary>
public interface IPhoneClientContract
{
    /// <summary>
    /// ログイン成功を通知する。
    /// </summary>
    /// <param name="myConnectionId">自身のConnectionId</param>
    Task ReceiveLoginSuccess(string myConnectionId);

    /// <summary>
    /// 着信を通知する。
    /// </summary>
    /// <param name="fromNumber">発信元の電話番号</param>
    /// <param name="callerConnectionId">発信者のConnectionId</param>
    Task ReceiveIncoming(string fromNumber, string callerConnectionId);

    /// <summary>
    /// 相手が応答したことを通知する。
    /// </summary>
    /// <param name="responderId">応答者のConnectionId</param>
    Task ReceiveAnswered(string responderId);

    /// <summary>
    /// 着信がキャンセルされたことを通知する（他のメンバーが応答済み）。
    /// </summary>
    /// <param name="callerConnectionId">発信者のConnectionId</param>
    Task ReceiveCancel(string callerConnectionId);

    /// <summary>
    /// 相手が着信を拒否したことを通知する。
    /// </summary>
    /// <param name="fromId">拒否した相手のConnectionId</param>
    Task ReceiveReject(string fromId);

    /// <summary>
    /// 相手が話中であることを通知する。
    /// </summary>
    /// <param name="fromId">話中の相手のConnectionId</param>
    Task ReceiveBusy(string fromId);

    /// <summary>
    /// 相手が通話を切断したことを通知する。
    /// </summary>
    /// <param name="fromId">切断した相手のConnectionId</param>
    Task ReceiveHangup(string fromId);

    /// <summary>
    /// 保留要求を通知する。
    /// </summary>
    /// <param name="fromId">保留を要求した相手のConnectionId</param>
    Task ReceiveHoldRequest(string fromId);

    /// <summary>
    /// 保留解除（再開）要求を通知する。
    /// </summary>
    /// <param name="fromId">再開を要求した相手のConnectionId</param>
    Task ReceiveResumeRequest(string fromId);
}
