using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Server;

public interface IServerRepository
{
    Task<ServerState?> GetServerStateAsync();
    Task SetServerStateAsync(ServerMode mode);
    Task<int> GetTimeOffset();
    Task SetTimeOffsetAsync(int timeOffset);
    Task SetSwitchMoveTimeAsync(int switchMoveTime);
    Task SetUseOneSecondRelayAsync(bool useOneSecondRelay);
}

