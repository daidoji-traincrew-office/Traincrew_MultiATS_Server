using System.Collections.Concurrent;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
/// インメモリの電話セッション接続マッピング管理クラス（Singleton）
/// </summary>
public class PhoneSessionStore
{
    // ConnectionId → 電話番号
    private readonly ConcurrentDictionary<string, string> _connectionToNumber = new();

    // 電話番号 → 接続中の ConnectionId セット
    private readonly ConcurrentDictionary<string, HashSet<string>> _groupMembers = new();

    // ConnectionId → UserID
    private readonly ConcurrentDictionary<string, string> _connectionToUserId = new();

    // UserID → 通話相手の UserID（Answer 後に双方向で設定）
    private readonly ConcurrentDictionary<string, string> _activeCallPartner = new();

    // グループメンバーへのアクセス用ロック
    private readonly object _groupLock = new();

    /// <summary>
    /// ConnectionId から電話番号を取得
    /// </summary>
    public string? GetNumberByConnectionId(string connectionId)
    {
        return _connectionToNumber.TryGetValue(connectionId, out var number) ? number : null;
    }

    /// <summary>
    /// 電話番号からグループメンバー（ConnectionIdセット）を取得
    /// </summary>
    public IReadOnlySet<string>? GetMembersByNumber(string number)
    {
        lock (_groupLock)
        {
            return _groupMembers.TryGetValue(number, out var members)
                ? new HashSet<string>(members)
                : null;
        }
    }

    /// <summary>
    /// ConnectionId から UserID を取得
    /// </summary>
    public string? GetUserIdByConnectionId(string connectionId)
    {
        return _connectionToUserId.TryGetValue(connectionId, out var userId) ? userId : null;
    }

    /// <summary>
    /// 通話相手の UserID を取得
    /// </summary>
    public string? GetCallPartnerUserId(string userId)
    {
        return _activeCallPartner.TryGetValue(userId, out var partnerId) ? partnerId : null;
    }

    /// <summary>
    /// 双方向に通話ペアを登録
    /// </summary>
    public void SetCallPartner(string userIdA, string userIdB)
    {
        _activeCallPartner[userIdA] = userIdB;
        _activeCallPartner[userIdB] = userIdA;
    }

    /// <summary>
    /// 自分と相手の両方の通話ペアを解除
    /// </summary>
    public void ClearCallPartner(string userId)
    {
        if (_activeCallPartner.TryRemove(userId, out var partnerId))
        {
            _activeCallPartner.TryRemove(partnerId, out _);
        }
    }

    /// <summary>
    /// ConnectionId、UserID、電話番号を登録
    /// </summary>
    public void Register(string connectionId, string userId, string number)
    {
        _connectionToNumber[connectionId] = number;
        _connectionToUserId[connectionId] = userId;

        lock (_groupLock)
        {
            if (!_groupMembers.ContainsKey(number))
            {
                _groupMembers[number] = [];
            }
            _groupMembers[number].Add(connectionId);
        }
    }

    /// <summary>
    /// ConnectionId を登録解除
    /// </summary>
    public void Unregister(string connectionId)
    {
        _connectionToUserId.TryRemove(connectionId, out _);

        if (!_connectionToNumber.TryRemove(connectionId, out var number))
        {
            return;
        }

        lock (_groupLock)
        {
            if (_groupMembers.TryGetValue(number, out var members))
            {
                members.Remove(connectionId);

                // グループが空になったら削除
                if (members.Count == 0)
                {
                    _groupMembers.TryRemove(number, out _);
                }
            }
        }
    }
}
