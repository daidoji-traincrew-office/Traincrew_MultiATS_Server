using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Scheduler;
using Traincrew_MultiATS_Server.Services.Cache;

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
    SchedulerManagerForServer schedulerManagerForServer,
    IMutexRepository mutexRepository,
    ICacheGate cacheGate) : IServerService
{
    public async Task<ServerMode> GetServerModeAsync()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(ServerService));
        return await GetServerModeAsyncWithoutLock();
    }

    public async Task<ServerMode> GetServerModeAsyncWithoutLock()
    {
        var mode = await serverRepository.GetModeAsync();
        if (mode == null)
        {
            throw new InvalidOperationException("ServerStateが存在しません。");
        }
        return mode.Value;
    }

    /// <summary>
    /// サーバモードを、プロセス内にキャッシュした値から返す。
    /// </summary>
    /// <remarks>
    /// ATSのホットパスから毎tick呼ばれるため用意している。使うのは CreateAtsData だけで、
    /// クライアントへのブロードキャスト(ServerModeScheduler)や旅客用プロセスは
    /// 従来の非キャッシュ版を使い続ける。
    ///
    /// mode を書くのは SetServerModeAsync だけ(司令卓経由)で、これはCrewプロセス内にあり
    /// 書き込み後にキャッシュを捨てる。連動装置プロセスは server_state の
    /// interlocking_heartbeat_at と is_all_signal_relay_raised しか書かず mode は読むだけなので、
    /// mode に限ればプロセスを跨いだ stale は起こらない。
    /// </remarks>
    public async Task<ServerMode> GetServerModeCachedAsync()
    {
        // factory には ...WithoutLock を渡すこと。nameof(ServerService) の
        // mutexを取るメソッドを渡すと SetServerModeAsync との間でロック順が反転する
        return await cacheGate.GetOrFillAsync(CacheKeys.ServerMode, GetServerModeAsyncWithoutLock);
    }

    public async Task SetServerModeAsync(ServerMode mode)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(ServerService));
        await serverRepository.SetServerStateAsync(mode);
        // DB書き込みの完了後に捨てる。cache:server:mode は別キーなのでデッドロックしない
        await cacheGate.InvalidateAsync(CacheKeys.ServerMode);
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

    public async Task<ulong?> GetSelectedDiagramIdAsync()
    {
        return await serverRepository.GetSelectedDiagramIdAsync();
    }

    public async Task SetSelectedDiagramIdAsync(ulong? diaId)
    {
        await serverRepository.SetSelectedDiagramIdAsync(diaId);
    }
}