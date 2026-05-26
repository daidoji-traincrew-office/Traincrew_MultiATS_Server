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
public class ServerServiceCacheTest
{
    private static (ServerService service, Mock<IServerRepository> repoMock, IMemoryCache cache) CreateService()
    {
        var repoMock = new Mock<IServerRepository>();
        var mutexMock = new Mock<IMutexRepository>();
        mutexMock
            .Setup(m => m.AcquireAsync(It.IsAny<string>()))
            .ReturnsAsync(new Mock<IAsyncDisposable>().Object);
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var schedulerManager = new SchedulerManager(scopeFactoryMock.Object, mutexMock.Object);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new ServerService(repoMock.Object, schedulerManager, mutexMock.Object, cache);
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
}
