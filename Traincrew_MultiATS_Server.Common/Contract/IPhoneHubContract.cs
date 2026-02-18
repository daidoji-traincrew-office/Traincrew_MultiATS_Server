using Traincrew_MultiATS_Server.Common.Models;

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
    Task<CallResponse> Call(string targetNumber);

    /// <summary>
    /// 着信に応答する。サーバーが着信元セッションを管理するため引数不要。
    /// </summary>
    Task Answer();

    /// <summary>
    /// 着信を拒否する。サーバーが着信元セッションを管理するため引数不要。
    /// </summary>
    Task Reject();

    /// <summary>
    /// 通話を切断する。サーバーがセッションを管理するため引数不要。
    /// </summary>
    Task Hangup();

    /// <summary>
    /// 通話を保留にする。サーバーがセッションを管理するため引数不要。
    /// </summary>
    Task Hold();

    /// <summary>
    /// 保留中の通話を再開する。サーバーがセッションを管理するため引数不要。
    /// </summary>
    Task Resume();
}

/// <summary>
/// 電話Hubのクライアントコールバック Contract。サーバーからクライアントへ通知される。
/// </summary>
public interface IPhoneClientContract
{
    /// <summary>
    /// 着信を通知する。
    /// </summary>
    /// <param name="fromNumber">発信元の電話番号</param>
    Task ReceiveIncoming(string fromNumber);

    /// <summary>
    /// 相手が応答したことを通知する。
    /// </summary>
    Task ReceiveAnswered();

    /// <summary>
    /// 着信がキャンセルされたことを通知する（他のメンバーが応答済み）。
    /// </summary>
    Task ReceiveCancel();

    /// <summary>
    /// 相手が着信を拒否したことを通知する。
    /// </summary>
    Task ReceiveReject();

    /// <summary>
    /// 相手が通話を切断したことを通知する。
    /// </summary>
    Task ReceiveHangup();

    /// <summary>
    /// 保留要求を通知する。
    /// </summary>
    Task ReceiveHoldRequest();

    /// <summary>
    /// 保留解除（再開）要求を通知する。
    /// </summary>
    Task ReceiveResumeRequest();
}
