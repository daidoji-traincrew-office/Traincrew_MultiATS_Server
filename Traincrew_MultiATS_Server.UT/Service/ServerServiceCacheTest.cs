using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Scheduler;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// ServerService の IMemoryCache を使ったキャッシュ動作のテスト
/// </summary>
/// <remarks>
/// IMutexRepository は Mock ではなく実物の <see cref="MutexRepository"/> を使う。
/// Mock だと何も直列化されず、キャッシュ充填と無効化の競合テストが素通りしてしまうため。
/// </remarks>
public class ServerServiceCacheTest
{
    private static (ServerService service, Mock<IServerRepository> repoMock, IMemoryCache cache) CreateService()
    {
        var repoMock = new Mock<IServerRepository>();
        // 実物のミューテックス(Crew/Passenger では Singleton 登録)
        var mutexRepository = new MutexRepository();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var schedulerManagerForServer = new SchedulerManagerForServer(scopeFactoryMock.Object, mutexRepository);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new ServerService(repoMock.Object, schedulerManagerForServer, mutexRepository, cache);
        return (service, repoMock, cache);
    }

    [Fact]
    public async Task GetServerModeCachedAsync_CalledTwice_RepositoryCalledOnce()
    {
        var (service, repoMock, _) = CreateService();
        repoMock
            .Setup(r => r.GetServerStateAsync())
            .ReturnsAsync(new ServerState { Mode = ServerMode.Private });

        var first = await service.GetServerModeCachedAsync();
        var second = await service.GetServerModeCachedAsync();

        Assert.Equal(ServerMode.Private, first);
        Assert.Equal(ServerMode.Private, second);
        repoMock.Verify(r => r.GetServerStateAsync(), Times.Once);
    }

    [Fact]
    public async Task SetServerModeAsync_InvalidatesCache_LatestValueReturned()
    {
        var (service, repoMock, _) = CreateService();
        var currentMode = ServerMode.Private;
        repoMock
            .Setup(r => r.GetServerStateAsync())
            .ReturnsAsync(() => new ServerState { Mode = currentMode });
        repoMock
            .Setup(r => r.SetServerStateAsync(It.IsAny<ServerMode>()))
            .Callback<ServerMode>(m => currentMode = m)
            .Returns(Task.CompletedTask);

        var initial = await service.GetServerModeCachedAsync();
        Assert.Equal(ServerMode.Private, initial);

        await service.SetServerModeAsync(ServerMode.Off);

        var afterSet = await service.GetServerModeCachedAsync();
        Assert.Equal(ServerMode.Off, afterSet);
    }

    [Fact]
    public async Task GetTimeOffsetAsync_CalledTwice_RepositoryCalledOnce()
    {
        var (service, repoMock, _) = CreateService();
        repoMock.Setup(r => r.GetTimeOffset()).ReturnsAsync(42);

        var first = await service.GetTimeOffsetAsync();
        var second = await service.GetTimeOffsetAsync();

        Assert.Equal(42, first);
        Assert.Equal(42, second);
        repoMock.Verify(r => r.GetTimeOffset(), Times.Once);
    }

    [Fact]
    public async Task SetTimeOffsetAsync_InvalidatesCache_RepositoryCalledAgain()
    {
        var (service, repoMock, _) = CreateService();
        repoMock.Setup(r => r.GetTimeOffset()).ReturnsAsync(42);

        _ = await service.GetTimeOffsetAsync();
        await service.SetTimeOffsetAsync(99);
        _ = await service.GetTimeOffsetAsync();

        repoMock.Verify(r => r.GetTimeOffset(), Times.Exactly(2));
    }

    [Fact]
    public async Task GetSelectedDiagramIdAsync_CalledTwice_RepositoryCalledOnce()
    {
        var (service, repoMock, _) = CreateService();
        repoMock.Setup(r => r.GetSelectedDiagramIdAsync()).ReturnsAsync((ulong)123);

        var first = await service.GetSelectedDiagramIdAsync();
        var second = await service.GetSelectedDiagramIdAsync();

        Assert.Equal((ulong)123, first);
        Assert.Equal((ulong)123, second);
        repoMock.Verify(r => r.GetSelectedDiagramIdAsync(), Times.Once);
    }

    [Fact]
    public async Task SetSelectedDiagramIdAsync_InvalidatesCache_RepositoryCalledAgain()
    {
        var (service, repoMock, _) = CreateService();
        repoMock.Setup(r => r.GetSelectedDiagramIdAsync()).ReturnsAsync((ulong)123);

        _ = await service.GetSelectedDiagramIdAsync();
        await service.SetSelectedDiagramIdAsync(456);
        _ = await service.GetSelectedDiagramIdAsync();

        repoMock.Verify(r => r.GetSelectedDiagramIdAsync(), Times.Exactly(2));
    }

    /// <summary>
    /// 競合の回帰テスト。
    /// キャッシュ充填の SELECT が実行されている最中に無効化(時刻オフセットの変更)が走っても、
    /// SELECT 完了後に陳腐化した値がキャッシュに残らないことを検証する。
    /// GetOrCreateAsync を使った修正前の実装では、ファクトリ実行中の Remove は何も消さず
    /// 陳腐化した値がそのまま格納されるため、このテストは落ちる。
    /// </summary>
    [Fact]
    public async Task SlowSelect_ConcurrentInvalidation_StaleValueIsNotCached()
    {
        var (service, repoMock, _) = CreateService();

        var current = 42;
        var selectStarted = new TaskCompletionSource();
        var releaseSelect = new TaskCompletionSource();
        var isFirstSelect = true;

        repoMock
            .Setup(r => r.GetTimeOffset())
            .Returns(async () =>
            {
                if (isFirstSelect)
                {
                    isFirstSelect = false;
                    // SELECT 開始時点のスナップショット(= この後の書き込みは見えない)
                    var snapshot = current;
                    selectStarted.TrySetResult();
                    // 「遅い SELECT」。この間に無効化が走る
                    await releaseSelect.Task;
                    return snapshot;
                }

                return current;
            });
        repoMock
            .Setup(r => r.SetTimeOffsetAsync(It.IsAny<int>()))
            .Callback<int>(v => current = v)
            .Returns(Task.CompletedTask);

        // 1. 遅い SELECT を開始させる
        var readTask = service.GetTimeOffsetAsync();
        await selectStarted.Task;

        // 2. その最中に指令卓由来の書き込み + キャッシュ無効化を走らせる
        var writeTask = service.SetTimeOffsetAsync(99);
        // 無効化がキャッシュ充填と衝突するタイミングまで進めるための猶予
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // 3. SELECT を完了させる(陳腐化した 42 が返る)
        releaseSelect.TrySetResult();
        Assert.Equal(42, await readTask);
        await writeTask;

        // 陳腐化した値がキャッシュに残っていないこと(残っていると TTL の 10 秒間反映されない)
        Assert.Equal(99, await service.GetTimeOffsetAsync());
    }
}
