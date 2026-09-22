using Moq;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// <see cref="OperationNotificationMasterStore"/> のライフサイクル(未ロード・差し替え・破棄)のテスト。
/// </summary>
/// <remarks>
/// このストアはDBを読まず、<see cref="IInterlockingObjectMasterStore"/> から派生スナップショットを組む。
/// そのため「連動装置マスタが載った後にReloadすること」という順序依存が唯一の壊しどころで、
/// ここではその前後の挙動を固定する。
/// IMutexRepository は <see cref="InterlockingObjectMasterStoreTest"/> と同じ理由で実物を使う。
/// </remarks>
public class OperationNotificationMasterStoreTest
{
    private static TrackCircuit TrackCircuit(ulong id, string? displayName) => new()
    {
        Id = id,
        Name = $"TC{id}",
        Type = ObjectType.TrackCircuit,
        OperationNotificationDisplayName = displayName
    };

    private static (OperationNotificationMasterStore store, Mock<IInterlockingObjectMasterStore> sourceMock)
        CreateStore()
    {
        var sourceMock = new Mock<IInterlockingObjectMasterStore>();
        var store = new OperationNotificationMasterStore(sourceMock.Object, new MutexRepository());
        return (store, sourceMock);
    }

    private static void SetupSource(Mock<IInterlockingObjectMasterStore> sourceMock,
        params InterlockingObject[] objects)
    {
        sourceMock.SetupGet(s => s.Current).Returns(new InterlockingObjectMaster(objects));
    }

    [Fact]
    public void Current_BeforeReload_Throws()
    {
        var (store, _) = CreateStore();

        // 黙って0件を返すと「告知器が1つも無い」と誤認され、全クライアントで告知器が消える
        Assert.Throws<InvalidOperationException>(() => store.Current);
    }

    [Fact]
    public async Task ReloadAsync_BuildsSnapshotFromInterlockingObjectMaster()
    {
        var (store, sourceMock) = CreateStore();
        SetupSource(sourceMock, TrackCircuit(1, "A"), TrackCircuit(2, "A"), TrackCircuit(3, null));

        var returned = await store.ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Same(returned, store.Current);
        Assert.Equal("A", store.Current.TryGetDisplayName([1, 2]));
        Assert.Null(store.Current.TryGetDisplayName([3]));
    }

    [Fact]
    public async Task ReloadAsync_IgnoresNonTrackCircuitObjects()
    {
        var (store, sourceMock) = CreateStore();
        // 連動装置マスタには軌道回路以外も載っている。混ざっても落ちないこと
        SetupSource(sourceMock,
            TrackCircuit(1, "A"),
            new Route { Id = 2, Name = "R2", Type = ObjectType.Route, TcName = "tc_R2", RouteType = RouteType.Arriving });

        await store.ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("A", store.Current.TryGetDisplayName([1]));
        Assert.Null(store.Current.TryGetDisplayName([2]));
    }

    [Fact]
    public async Task ReloadAsync_BeforeInterlockingObjectMasterLoaded_Throws()
    {
        var (store, sourceMock) = CreateStore();
        // InitDbHostedService で連動装置マスタより先に呼ぶと、この経路に落ちる。
        // 不完全なマスタから索引を組んで黙って動き続けるより、起動を止める方がよい
        sourceMock
            .SetupGet(s => s.Current)
            .Throws(new InvalidOperationException("InterlockingObjectMaster が未ロードです。"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.ReloadAsync(TestContext.Current.CancellationToken));
        Assert.Throws<InvalidOperationException>(() => store.Current);
    }

    [Fact]
    public async Task ReloadAsync_FailedLoad_KeepsPreviousSnapshot()
    {
        var (store, sourceMock) = CreateStore();
        SetupSource(sourceMock, TrackCircuit(1, "OLD"));
        await store.ReloadAsync(TestContext.Current.CancellationToken);

        sourceMock.SetupGet(s => s.Current).Throws(new InvalidOperationException("連動装置マスタが落ちた"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.ReloadAsync(TestContext.Current.CancellationToken));

        Assert.Equal("OLD", store.Current.TryGetDisplayName([1]));
    }

    [Fact]
    public async Task ReloadAsync_PicksUpInterlockingObjectMasterChange()
    {
        var (store, sourceMock) = CreateStore();
        SetupSource(sourceMock, TrackCircuit(1, "OLD"));
        await store.ReloadAsync(TestContext.Current.CancellationToken);

        // 連動装置マスタ側が差し替わっても、こちらを叩くまでは古いまま
        SetupSource(sourceMock, TrackCircuit(1, "NEW"));
        Assert.Equal("OLD", store.Current.TryGetDisplayName([1]));

        await store.ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("NEW", store.Current.TryGetDisplayName([1]));
    }

    [Fact]
    public async Task Unload_MakesCurrentThrowAgain()
    {
        var (store, sourceMock) = CreateStore();
        SetupSource(sourceMock, TrackCircuit(1, "A"));
        await store.ReloadAsync(TestContext.Current.CancellationToken);

        store.Unload();

        Assert.Throws<InvalidOperationException>(() => store.Current);
    }
}
