namespace Traincrew_MultiATS_Server.Services.Cache;

/// <summary>
/// <see cref="ICacheGate"/> で使うキャッシュキー。
/// </summary>
/// <remarks>
/// キーはここに集約する。<see cref="ICacheGate"/> は項目ごとに別のmutexを取るので、
/// キーを取り違えると無効化が効かなくなる。
/// </remarks>
public static class CacheKeys
{
    /// <summary>server_state.mode。ServerState エンティティ丸ごとを載せてはならない(ICacheGateの注意書き参照)</summary>
    public const string ServerMode = "server:mode";

    /// <summary>接続拒否済みユーザーidの集合。userId単位では持たない</summary>
    public const string BannedUserIds = "ban:userIds";

    /// <summary>運転告知器の状態(告知器名 → 表示内容)の全件</summary>
    public const string OperationNotificationStates = "operationNotification:states";
}
