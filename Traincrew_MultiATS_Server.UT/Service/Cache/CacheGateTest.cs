using Microsoft.Extensions.Caching.Memory;
using Traincrew_MultiATS_Server.HostedService;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Services.Cache;

namespace Traincrew_MultiATS_Server.UT.Service.Cache;

/// <summary>
/// <see cref="CacheGate"/> のテスト。
/// </summary>
/// <remarks>
/// IMutexRepository と IMemoryCache はモックではなく実物を使う。
/// このクラスの振る舞いはmutexとキャッシュの相互作用そのものなので、
/// モックにすると一番見たいもの(充填中の無効化が取りこぼされないこと)が見られない。
/// </remarks>
public class CacheGateTest : IDisposable
{
    private const string Key = "test:key";

    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly InitializationState _initializationState = new();

    private CacheGate CreateGate() => new(_cache, new MutexRepository(), _initializationState);

    private CacheGate CreateInitializedGate()
    {
        _initializationState.MarkInitialized();
        return CreateGate();
    }

    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "初期化完了後は2回目の読みでファクトリが走らないこと")]
    public async Task GetOrFillAsync_AfterInitialized_CachesValue()
    {
        var gate = CreateInitializedGate();
        var calls = 0;

        Assert.Equal(1, await gate.GetOrFillAsync(Key, () => Task.FromResult(++calls)));
        Assert.Equal(1, await gate.GetOrFillAsync(Key, () => Task.FromResult(++calls)));
        Assert.Equal(1, calls);
    }

    [Fact(DisplayName = "初期化が完走するまではキャッシュに載せないこと")]
    public async Task GetOrFillAsync_BeforeInitialized_DoesNotCache()
    {
        // 初期化途中の不完全な状態がTTL分居座るのを防ぐ
        var gate = CreateGate();
        var calls = 0;

        await gate.GetOrFillAsync(Key, () => Task.FromResult(++calls));
        await gate.GetOrFillAsync(Key, () => Task.FromResult(++calls));

        Assert.Equal(2, calls);
    }

    [Fact(DisplayName = "無効化した後はファクトリが再度走ること")]
    public async Task InvalidateAsync_ForcesRefill()
    {
        var gate = CreateInitializedGate();
        var value = "old";

        Assert.Equal("old", await gate.GetOrFillAsync(Key, () => Task.FromResult(value)));
        value = "new";
        await gate.InvalidateAsync(Key);

        Assert.Equal("new", await gate.GetOrFillAsync(Key, () => Task.FromResult(value)));
    }

    [Fact(DisplayName = "充填中に着弾した無効化が取りこぼされないこと")]
    public async Task InvalidateAsync_DuringFill_IsNotLost()
    {
        // IMemoryCache.GetOrCreateAsync を使うとファクトリ実行中はエントリが確定せず、
        // この Remove が no-op になって書き込み前の値がTTLいっぱい居座る。
        // その回帰を防ぐためのテスト。
        var gate = CreateInitializedGate();
        var factoryEntered = new TaskCompletionSource();
        var releaseFactory = new TaskCompletionSource();
        var value = "old";

        var filling = gate.GetOrFillAsync(Key, async () =>
        {
            // SELECTが書き込み前の値を読んだ状況を模す
            var snapshot = value;
            factoryEntered.SetResult();
            await releaseFactory.Task;
            return snapshot;
        });

        // ファクトリ(=SELECT相当)が走っている最中に書き込み+無効化が着弾する
        await factoryEntered.Task;
        value = "new";
        var invalidating = gate.InvalidateAsync(Key);

        releaseFactory.SetResult();
        // 充填中の呼び出し自身は、その時点で読んだ値を返してよい
        Assert.Equal("old", await filling);
        await invalidating;

        // 肝心なのはこの後。無効化が効いていれば新しい値が読める
        Assert.Equal("new", await gate.GetOrFillAsync(Key, () => Task.FromResult(value)));
    }

    [Fact(DisplayName = "同一キーに同時に来てもファクトリは1回だけ走ること")]
    public async Task GetOrFillAsync_Concurrent_RunsFactoryOnce()
    {
        var gate = CreateInitializedGate();
        var calls = 0;

        var results = await Task.WhenAll(Enumerable.Range(0, 100).Select(_ =>
            gate.GetOrFillAsync(Key, async () =>
            {
                Interlocked.Increment(ref calls);
                await Task.Yield();
                return 42;
            })));

        Assert.Equal(1, calls);
        Assert.All(results, result => Assert.Equal(42, result));
    }

    [Fact(DisplayName = "nullもキャッシュヒット扱いになること")]
    public async Task GetOrFillAsync_NullValue_IsCached()
    {
        // GetSelectedDiagramIdAsync のように null が正常値の項目があるため、
        // 「nullを載せたらミス扱いになってファクトリが毎回走る」ことがないのを固定する
        var gate = CreateInitializedGate();
        var calls = 0;

        Assert.Null(await gate.GetOrFillAsync<ulong?>(Key, () =>
        {
            calls++;
            return Task.FromResult<ulong?>(null);
        }));
        Assert.Null(await gate.GetOrFillAsync<ulong?>(Key, () =>
        {
            calls++;
            return Task.FromResult<ulong?>(null);
        }));

        Assert.Equal(1, calls);
    }

    [Fact(DisplayName = "キーごとに独立していること")]
    public async Task GetOrFillAsync_DifferentKeys_AreIndependent()
    {
        var gate = CreateInitializedGate();

        Assert.Equal(1, await gate.GetOrFillAsync("a", () => Task.FromResult(1)));
        Assert.Equal(2, await gate.GetOrFillAsync("b", () => Task.FromResult(2)));

        await gate.InvalidateAsync("a");

        Assert.Equal(3, await gate.GetOrFillAsync("a", () => Task.FromResult(3)));
        Assert.Equal(2, await gate.GetOrFillAsync("b", () => Task.FromResult(99)));
    }
}
