using Microsoft.Extensions.Caching.Memory;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Scheduler;

namespace Traincrew_MultiATS_Server.Services;

public interface IServerService
{
    Task<ServerMode> GetServerModeAsync();
    Task<ServerMode> GetServerModeAsyncWithoutLock();
    Task<ServerMode> GetServerModeCachedAsync();
    Task SetServerModeAsync(ServerMode mode);
    Task UpdateSchedulerAsync();
    Task<int> GetTimeOffsetAsync();
    Task SetTimeOffsetAsync(int timeOffset);
    Task SetSwitchMoveTimeAsync(int switchMoveTime);
    Task SetUseOneSecondRelayAsync(bool useOneSecondRelay);
    Task<ulong?> GetSelectedDiagramIdAsync();
    Task SetSelectedDiagramIdAsync(ulong? diaId);
}

public class ServerService(
    IServerRepository serverRepository,
    SchedulerManager schedulerManager,
    IMutexRepository mutexRepository,
    IMemoryCache cache) : IServerService
{
    private const string CacheKeyServerMode = "servermode";
    private const string CacheKeyTimeOffset = "timeoffset";
    private const string CacheKeySelectedDiaId = "selectedDiaId";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(10);

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

    public async Task<ServerMode> GetServerModeCachedAsync()
    {
        return await cache.GetOrCreateAsync(CacheKeyServerMode, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await GetServerModeAsyncWithoutLock();
        });
    }

    public async Task SetServerModeAsync(ServerMode mode)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(ServerService));
        await serverRepository.SetServerStateAsync(mode);
        cache.Remove(CacheKeyServerMode);
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

    public virtual async Task<int> GetTimeOffsetAsync()
    {
        return (await cache.GetOrCreateAsync(CacheKeyTimeOffset, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await serverRepository.GetTimeOffset();
        }))!;
    }

    public async Task SetTimeOffsetAsync(int timeOffset)
    {
        await serverRepository.SetTimeOffsetAsync(timeOffset);
        cache.Remove(CacheKeyTimeOffset);
    }

    public async Task SetSwitchMoveTimeAsync(int switchMoveTime)
    {
        await serverRepository.SetSwitchMoveTimeAsync(switchMoveTime);
    }

    public async Task SetUseOneSecondRelayAsync(bool useOneSecondRelay)
    {
        await serverRepository.SetUseOneSecondRelayAsync(useOneSecondRelay);
    }

    public async Task<ulong?> GetSelectedDiagramIdAsync()
    {
        return await cache.GetOrCreateAsync(CacheKeySelectedDiaId, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await serverRepository.GetSelectedDiagramIdAsync();
        });
    }

    public async Task SetSelectedDiagramIdAsync(ulong? diaId)
    {
        await serverRepository.SetSelectedDiagramIdAsync(diaId);
        cache.Remove(CacheKeySelectedDiaId);
    }
}
