using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Server;

public interface IServerRepository
{
    Task<ServerState?> GetServerStateAsync();
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

