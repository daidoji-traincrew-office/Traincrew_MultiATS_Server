using Microsoft.Extensions.DependencyInjection;
using Moq;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// <see cref="InterlockingObjectMasterStore"/> のライフサイクル(未ロード・差し替え・破棄)のテスト。
/// </summary>
/// <remarks>
/// IMutexRepository は Mock ではなく実物の <see cref="MutexRepository"/> を使う。
/// Mock だとリロード同士が直列化されず、排他のテストが素通りしてしまうため。
/// </remarks>
public class InterlockingObjectMasterStoreTest
{
    private static TrackCircuit TrackCircuit(ulong id, string name) => new()
    {
        Id = id,
        Name = name,
        Type = ObjectType.TrackCircuit
    };

    private static (InterlockingObjectMasterStore store, Mock<IInterlockingObjectRepository> repoMock) CreateStore()
    {
        var repoMock = new Mock<IInterlockingObjectRepository>();
        // Singleton から Scoped なリポジトリを取る本番と同じ経路を通す
        var provider = new ServiceCollection()
            .AddScoped(_ => repoMock.Object)
            .BuildServiceProvider();
        var store = new InterlockingObjectMasterStore(
            provider.GetRequiredService<IServiceScopeFactory>(), new MutexRepository());
        return (store, repoMock);
    }

    [Fact]
    public void Current_BeforeReload_Throws()
    {
        var (store, _) = CreateStore();

        // 黙って0件を返すと「軌道回路が1つも無い」と誤認されるので、例外にする
        Assert.Throws<InvalidOperationException>(() => store.Current);
    }

    [Fact]
    public async Task ReloadAsync_LoadsSnapshot()
    {
        var (store, repoMock) = CreateStore();
        repoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TrackCircuit(1, "TC1")]);

        var returned = await store.ReloadAsync();

        Assert.Same(returned, store.Current);
        Assert.Equal(["TC1"], store.Current.All.Select(o => o.Name));
    }

    [Fact]
    public async Task ReloadAsync_WhileLoading_ReadersKeepSeeingOldSnapshot()
    {
        var (store, repoMock) = CreateStore();
        var secondLoadStarted = new TaskCompletionSource();
        var secondLoadCanFinish = new TaskCompletionSource();
        var callCount = 0;
        repoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                if (Interlocked.Increment(ref callCount) == 1)
                {
                    return [TrackCircuit(1, "OLD")];
                }

                secondLoadStarted.SetResult();
                await secondLoadCanFinish.Task;
                return [TrackCircuit(2, "NEW")];
            });

        await store.ReloadAsync();
        var reloading = store.ReloadAsync();
        await secondLoadStarted.Task;

        // 読み込み中は旧スナップショットが見え続ける
        Assert.Equal(["OLD"], store.Current.All.Select(o => o.Name));

        secondLoadCanFinish.SetResult();
        await reloading;

        // 完了後は原子的に新しい方へ切り替わる
        Assert.Equal(["NEW"], store.Current.All.Select(o => o.Name));
    }

    [Fact]
    public async Task ReloadAsync_FailedLoad_KeepsPreviousSnapshot()
    {
        var (store, repoMock) = CreateStore();
        var callCount = 0;
        repoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Interlocked.Increment(ref callCount) == 1
                ? Task.FromResult<List<InterlockingObject>>([TrackCircuit(1, "OLD")])
                : Task.FromException<List<InterlockingObject>>(new InvalidOperationException("DB落ちた")));

        await store.ReloadAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ReloadAsync());

        Assert.Equal(["OLD"], store.Current.All.Select(o => o.Name));
    }

    [Fact]
    public async Task Unload_MakesCurrentThrowAgain()
    {
        var (store, repoMock) = CreateStore();
        repoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TrackCircuit(1, "TC1")]);
        await store.ReloadAsync();

        store.Unload();

        Assert.Throws<InvalidOperationException>(() => store.Current);
    }
}
