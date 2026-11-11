using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Server;

public interface IServerRepository
{
    Task<ServerState?> GetServerStateAsync();

    /// <summary>
    /// server_state.mode だけを読む。
    /// </summary>
    /// <remarks>
    /// ServerState 全体を読むと、連動装置プロセスが書く interlocking_heartbeat_at や
    /// is_all_signal_relay_raised まで持ち回ることになる。前者は2秒のタイムアウトで
    /// フェイルセーフ判定に使われており、キャッシュに載せてはならない値である。
    /// mode を扱う経路はこのメソッドを使い、ServerState エンティティを持ち回らない。
    /// </remarks>
    /// <returns>mode。server_state の行自体が無ければ null</returns>
    Task<ServerMode?> GetModeAsync();
    Task SetServerStateAsync(ServerMode mode);
    Task<int> GetTimeOffset();
    Task SetTimeOffsetAsync(int timeOffset);
    Task AddServerStateAsync(ServerState serverState, CancellationToken cancellationToken = default);
    Task SetSwitchMoveTimeAsync(int switchMoveTime);
    Task SetUseOneSecondRelayAsync(bool useOneSecondRelay);
    Task SetIsAllSignalRelayRaisedAsync(RaiseDropWithForce raiseDropWithForce);
    Task<ulong?> GetSelectedDiagramIdAsync();
    Task SetSelectedDiagramIdAsync(ulong? diaId);

    /// <summary>
    /// 連動サーバーの生存確認用ハートビートのみを更新する
    /// (server_stateは2プロセスが別々の列を書くため、列限定更新にすること)
    /// </summary>
    /// <param name="heartbeatAt">ハートビート時刻</param>
    Task SetInterlockingHeartbeatAtAsync(DateTime heartbeatAt);
}

