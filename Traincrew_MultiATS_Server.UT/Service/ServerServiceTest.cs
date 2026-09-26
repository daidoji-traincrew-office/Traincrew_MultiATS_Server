using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.HostedService;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Scheduler;
using Traincrew_MultiATS_Server.Services;
using Traincrew_MultiATS_Server.Services.Cache;
using Traincrew_MultiATS_Server.UT.TestHelpers;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// サーバモードのキャッシュのテスト。
/// </summary>
/// <remarks>
/// サーバモードは間違うと最も痛い項目なので、次の3つを固定する:
/// 1. SetServerModeAsync が確実に無効化すること(意図しないモードが返らないこと)
/// 2. キャッシュするのは mode の値だけで、ServerState エンティティを持ち回らないこと
/// 3. 非キャッシュ版(ブロードキャストや旅客用プロセスが使う)がキャッシュを読まないこと
/// </remarks>
public class ServerServiceTest : ServiceTestBase, IDisposable
{
    private readonly Mock<IServerRepository> _serverRepositoryMock = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly InitializationState _initializationState = new();

    /// <summary>DB上の server_state.mode を模した状態</summary>
    private ServerMode? _mode = ServerMode.Private;

    public ServerServiceTest()
    {
        _initializationState.MarkInitialized();

        _serverRepositoryMock.Setup(r => r.GetModeAsync()).ReturnsAsync(() => _mode);
        _serverRepositoryMock
            .Setup(r => r.SetServerStateAsync(It.IsAny<ServerMode>()))
            .Callback<ServerMode>(mode => _mode = mode)
            .Returns(Task.CompletedTask);
    }

    protected override void ConfigureTestServices(ServiceCollection services)
    {
        services.AddAllMocks();
        services.ReplaceMock(_serverRepositoryMock);
        services.AddSingleton<ICacheGate>(
            _ => new CacheGate(_cache, new MutexRepository(), _initializationState));
        // SchedulerManagerForServer は具象クラスなので AddAllMocks に含まれない。
        // モードをOffにする経路しか使わないため、スケジューラが実際に起動することはない
        // (SchedulerManager.Stop は未起動なら何もしない。逆にStartは Scheduler の
        //  コンストラクタでバックグラウンドループを開始してしまうのでUTでは踏めない)
        services.AddSingleton<SchedulerManagerForServer>(
            _ => new(new Mock<IServiceScopeFactory>().Object, new MutexRepository()));
        services.UseRealService<IServerService, ServerService>();
    }

    public new void Dispose()
    {
        _cache.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "キャッシュ版は2回目以降DBを読まないこと")]
    public async Task GetServerModeCachedAsync_CachesValue()
    {
        var service = GetService<IServerService>();

        Assert.Equal(ServerMode.Private, await service.GetServerModeCachedAsync());
        Assert.Equal(ServerMode.Private, await service.GetServerModeCachedAsync());

        _serverRepositoryMock.Verify(r => r.GetModeAsync(), Times.Once);
    }

    [Fact(DisplayName = "モードを変更した直後からキャッシュ版が新しいモードを返すこと")]
    public async Task SetServerModeAsync_InvalidatesCache()
    {
        // 「キャッシュが残って意図しないサーバーモードが返る」ことを防げているかの本体
        var service = GetService<IServerService>();
        Assert.Equal(ServerMode.Private, await service.GetServerModeCachedAsync());

        await service.SetServerModeAsync(ServerMode.Off);

        Assert.Equal(ServerMode.Off, await service.GetServerModeCachedAsync());
    }

    [Fact(DisplayName = "モード変更がサービスのmutexの内側で無効化してもデッドロックしないこと")]
    public async Task SetServerModeAsync_DoesNotDeadlock()
    {
        // SetServerModeAsync は nameof(ServerService) を保持したまま cache:server:mode を取る。
        // ロック順が一方向である限りデッドロックしない(MutexRepositoryは再入不可)
        var service = GetService<IServerService>();

        var task = service.SetServerModeAsync(ServerMode.Off);

        Assert.Equal(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))));
        await task;
        Assert.Equal(ServerMode.Off, await service.GetServerModeCachedAsync());
    }

    [Fact(DisplayName = "非キャッシュ版はキャッシュを読まずに毎回DBを見ること")]
    public async Task GetServerModeAsyncWithoutLock_DoesNotUseCache()
    {
        // ServerModeScheduler(250msブロードキャスト)と旅客用プロセスが使う経路
        var service = GetService<IServerService>();
        Assert.Equal(ServerMode.Private, await service.GetServerModeCachedAsync());

        // キャッシュを無効化せずにDBだけを書き換える
        _mode = ServerMode.Off;

        Assert.Equal(ServerMode.Off, await service.GetServerModeAsyncWithoutLock());
        Assert.Equal(ServerMode.Off, await service.GetServerModeAsync());
        // キャッシュ版はまだ古い値(=上の2つがキャッシュ経由でないことの裏返し)
        Assert.Equal(ServerMode.Private, await service.GetServerModeCachedAsync());
    }

    [Fact(DisplayName = "ServerState エンティティを読まずに mode だけを読むこと")]
    public async Task GetServerMode_ReadsOnlyMode()
    {
        // ServerState 全体を読むと、連動装置プロセスが1Hzで書く interlocking_heartbeat_at まで
        // キャッシュに載ってしまう。あの列は2秒のタイムアウトでフェイルセーフ判定に使われている
        var service = GetService<IServerService>();

        await service.GetServerModeCachedAsync();
        await service.GetServerModeAsyncWithoutLock();

        _serverRepositoryMock.Verify(r => r.GetServerStateAsync(), Times.Never);
    }

    [Fact(DisplayName = "server_stateの行が無いときは例外になること")]
    public async Task GetServerMode_WhenNoRow_Throws()
    {
        _mode = null;
        var service = GetService<IServerService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetServerModeCachedAsync());
    }
}
