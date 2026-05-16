using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Scheduler;

namespace Traincrew_MultiATS_Server.Services;

public interface IServerService
{
    Task<ServerMode> GetServerModeAsync();
    Task<ServerMode> GetServerModeAsyncWithoutLock();
    Task SetServerModeAsync(ServerMode mode);
    Task UpdateSchedulerAsync();
    Task<int> GetTimeOffsetAsync();
    Task SetTimeOffsetAsync(int timeOffset);
    Task SetSwitchMoveTimeAsync(int switchMoveTime);
    Task SetUseOneSecondRelayAsync(bool useOneSecondRelay);
}

public class ServerService(
    IServerRepository serverRepository, 
    SchedulerManager schedulerManager,
    IMutexRepository mutexRepository) : IServerService
{
    public async Task<ServerMode> GetServerModeAsync()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(ServerService));
        return await GetServerModeAsyncWithoutLock();
    }

    public async Task<ServerMode> GetServerModeAsyncWithoutLock()
    {
        var state = await serverRepository.GetServerStateAsync();
        if (state == null)
        {
            throw new InvalidOperationException("ServerStateが存在しません。");
        }
        return state.Mode;
    }

    public async Task SetServerModeAsync(ServerMode mode)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(ServerService));
        await serverRepository.SetServerStateAsync(mode);
        await UpdateSchedulerAsyncWithoutLock();
    }

    public async Task UpdateSchedulerAsync()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(ServerService));
        await UpdateSchedulerAsyncWithoutLock();
    }

    private async Task UpdateSchedulerAsyncWithoutLock()
    {
        var mode = await GetServerModeAsyncWithoutLock();
        if (mode == ServerMode.Off)
        {
            await schedulerManager.Stop();
        }
        else
        {
            await schedulerManager.Start();
        }
    }

    public async Task<int> GetTimeOffsetAsync()
    {
        return await serverRepository.GetTimeOffset();
    }

    public async Task SetTimeOffsetAsync(int timeOffset)
    {
        await serverRepository.SetTimeOffsetAsync(timeOffset);
    }

    public async Task SetSwitchMoveTimeAsync(int switchMoveTime)
    {
        await serverRepository.SetSwitchMoveTimeAsync(switchMoveTime);
    }

    public async Task SetUseOneSecondRelayAsync(bool useOneSecondRelay)
    {
        await serverRepository.SetUseOneSecondRelayAsync(useOneSecondRelay);
    }
}