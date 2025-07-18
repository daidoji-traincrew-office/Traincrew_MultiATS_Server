using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Scheduler;

namespace Traincrew_MultiATS_Server.Services;

public class ServerService(
    IServerRepository serverRepository, 
    SchedulerManager schedulerManager,
    IMutexRepository mutexRepository)
{
    public async Task<ServerMode> GetServerStateAsync()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(ServerService));
        var state = await serverRepository.GetServerStateAsync();
        if (state == null)
        {
            throw new InvalidOperationException("ServerStateが存在しません。");
        }
        return state.Mode;
    }

    public async Task SetServerStateAsync(ServerMode mode)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(ServerService));
        await serverRepository.SetServerStateAsync(mode);
        await UpdateSchedulerAsync();
    }

    public async Task UpdateSchedulerAsync()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(ServerService));
        await UpdateSchedulerAsyncWithoutLock();
    }

    private async Task UpdateSchedulerAsyncWithoutLock()
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