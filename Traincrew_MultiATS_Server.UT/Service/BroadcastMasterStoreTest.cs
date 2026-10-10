using Microsoft.Extensions.DependencyInjection;
using Moq;
using Traincrew_MultiATS_Server.Repositories.BroadcastMaster;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// <see cref="BroadcastMasterStore"/> のロード・未ロード・Unloadのテスト。
/// 再ロードの機構は無い(マスタを更新したら再起動する)ので、ロードは起動時の1回を想定する。
/// </summary>
public class BroadcastMasterStoreTest
{
    private static BroadcastMasterData CreateMaster(params string[] stationIds) => new(
        stationIds, [], new Dictionary<string, IReadOnlyList<string>>(),
        new Dictionary<string, IReadOnlyList<ulong>>(), [], []);

    private static BroadcastMasterStore CreateStore(BroadcastMasterData master)
    {
        var repository = new Mock<IBroadcastMasterRepository>();
        repository.Setup(r => r.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(master);
        var provider = new ServiceCollection().AddScoped(_ => repository.Object).BuildServiceProvider();
        return new(provider.GetRequiredService<IServiceScopeFactory>(), new MutexRepository());
    }

    [Fact(DisplayName = "ロード前にCurrentを読むと例外")]
    public void Current_BeforeReload_Throws()
    {
        var store = CreateStore(CreateMaster());

        Assert.Throws<InvalidOperationException>(() => store.Current);
    }

    [Fact(DisplayName = "ロード後はCurrentが同じスナップショットを返し、Unloadで未ロードに戻る")]
    public async Task ReloadThenUnload()
    {
        var master = CreateMaster("TH58");
        var store = CreateStore(master);

        var loaded = await store.ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Same(master, loaded);
        Assert.Same(master, store.Current);
        Assert.Same(store.Current, store.Current);

        store.Unload();

        Assert.Throws<InvalidOperationException>(() => store.Current);
    }
}
