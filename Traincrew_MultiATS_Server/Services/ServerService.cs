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
    Task<int> GetTimeOffsetAsyncWithoutCache();
    Task SetTimeOffsetAsync(int timeOffset);
    Task SetSwitchMoveTimeAsync(int switchMoveTime);
    Task SetUseOneSecondRelayAsync(bool useOneSecondRelay);
    Task<ulong?> GetSelectedDiagramIdAsync();
    Task<ulong?> GetSelectedDiagramIdAsyncWithoutCache();
    Task SetSelectedDiagramIdAsync(ulong? diaId);
}

public class ServerService(
    IServerRepository serverRepository,
    SchedulerManagerForServer schedulerManagerForServer,
    IMutexRepository mutexRepository,
    IMemoryCache cache) : IServerService
{
    private const string CacheKeyServerMode = "servermode";
    private const string CacheKeyTimeOffset = "timeoffset";
    private const string CacheKeySelectedDiaId = "selectedDiaId";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(10);

    /// <summary>
    /// キャッシュの充填と無効化を直列化して取得する。
    /// </summary>
    /// <remarks>
    /// GetOrCreateAsync だとファクトリ実行中に走った Remove は何も消さず、その後で陳腐化した値が
    /// TTL 分だけ格納されてしまう(サーバモードを Off にしても最大 TTL の間反映されない)ため、
    /// ミューテックスで充填と無効化を直列化する。
    /// </remarks>
    private async Task<T> GetCachedAsync<T>(string cacheKey, Func<Task<T>> factory)
    {
        // ヒット時はここで終わり(ミューテックスに触れない)
        if (cache.TryGetValue(cacheKey, out T? cached))
        {
            return cached!;
        }

        await using var mutex = await mutexRepository.AcquireAsync(CacheMutexKey(cacheKey));
        // ゲート取得後に再チェック(同時ミス時に SELECT が並走するのを防ぐ)
        if (cache.TryGetValue(cacheKey, out cached))
        {
            return cached!;
        }

        var value = await factory();
        cache.Set(cacheKey, value, CacheTtl);
        return value;
    }

    /// <summary>
    /// キャッシュを破棄する。必ず DB 書き込みの完了後に呼ぶこと。
    /// </summary>
    private async Task InvalidateCacheAsync(string cacheKey)
    {
        await using var mutex = await mutexRepository.AcquireAsync(CacheMutexKey(cacheKey));
        cache.Remove(cacheKey);
    }

    private static string CacheMutexKey(string cacheKey) => $"{cacheKey}:mutex";

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
        return await GetCachedAsync(CacheKeyServerMode, GetServerModeAsyncWithoutLock);
    }

    public async Task SetServerModeAsync(ServerMode mode)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(ServerService));
        await serverRepository.SetServerStateAsync(mode);
        await InvalidateCacheAsync(CacheKeyServerMode);
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
            await schedulerManagerForServer.Stop();
        }
        else
        {
            await schedulerManagerForServer.Start();
        }
    }

    public virtual async Task<int> GetTimeOffsetAsync()
    {
        return await GetCachedAsync(CacheKeyTimeOffset, serverRepository.GetTimeOffset);
    }

    /// <summary>
    /// 時刻オフセットをキャッシュを経由せずDBから取得する。
    /// </summary>
    /// <remarks>
    /// キャッシュの無効化は書き込みを行ったプロセス内でしか効かず、書き込み経路はすべて Crew 側にある。
    /// そのため旅客用プロセスがキャッシュを読むと、指令卓の操作が TTL 分だけ旅客に反映されない。
    /// 旅客APIの呼び出し頻度は ATS と比べて圧倒的に少なく毎回DBを読んでも負荷にならないため、
    /// 旅客用プロセスからはこちらを呼ぶこと。
    /// </remarks>
    public async Task<int> GetTimeOffsetAsyncWithoutCache()
    {
        return await serverRepository.GetTimeOffset();
    }

    public async Task SetTimeOffsetAsync(int timeOffset)
    {
        await serverRepository.SetTimeOffsetAsync(timeOffset);
        await InvalidateCacheAsync(CacheKeyTimeOffset);
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
        return await GetCachedAsync(CacheKeySelectedDiaId, serverRepository.GetSelectedDiagramIdAsync);
    }

    /// <summary>
    /// 選択中のダイヤIDをキャッシュを経由せずDBから取得する。
    /// 旅客用プロセスから呼ぶ理由は <see cref="GetTimeOffsetAsyncWithoutCache"/> を参照。
    /// </summary>
    public async Task<ulong?> GetSelectedDiagramIdAsyncWithoutCache()
    {
        return await serverRepository.GetSelectedDiagramIdAsync();
    }

    public async Task SetSelectedDiagramIdAsync(ulong? diaId)
    {
        await serverRepository.SetSelectedDiagramIdAsync(diaId);
        await InvalidateCacheAsync(CacheKeySelectedDiaId);
    }
}
