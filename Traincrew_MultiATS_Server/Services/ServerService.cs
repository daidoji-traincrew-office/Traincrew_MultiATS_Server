using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Scheduler;

namespace Traincrew_MultiATS_Server.Services;

public class ServerService(IServerRepository serverRepository, SchedulerManager schedulerManager)
{
    public async Task<ServerMode> GetServerStateAsync()
    {
        var state = await serverRepository.GetServerStateAsync();
        if (state == null)
        {
            throw new InvalidOperationException("ServerStateが存在しません。");
        }
        return state.Mode;
    }

    public async Task SetServerStateAsync(ServerMode mode)
    {
        await serverRepository.SetServerStateAsync(mode);
        await UpdateSchedulerAsync();
    }

    public async Task UpdateSchedulerAsync()
    {
        var mode = await GetServerStateAsync();
        if (mode == ServerMode.Off)
        {
            await schedulerManager.Stop();
        }
        else
        {
            await schedulerManager.Start();
        }
    }
}