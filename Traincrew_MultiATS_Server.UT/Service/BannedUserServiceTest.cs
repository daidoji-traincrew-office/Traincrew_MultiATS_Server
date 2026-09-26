using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Traincrew_MultiATS_Server.HostedService;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.UserDisconnection;
using Traincrew_MultiATS_Server.Services;
using Traincrew_MultiATS_Server.Services.Cache;
using Traincrew_MultiATS_Server.UT.TestHelpers;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// 接続拒否判定のキャッシュのテスト。
/// </summary>
/// <remarks>
/// キャッシュ本体の振る舞いは CacheGateTest が見ている。ここで固定したいのは
/// 「BAN/UNBANが確実に無効化を呼ぶこと」と「非キャッシュ版がキャッシュを読まないこと」。
/// ICacheGate は実物を使う(モックにすると無効化が効いたかどうかが見えない)。
/// </remarks>
public class BannedUserServiceTest : ServiceTestBase, IDisposable
{
    private const ulong UserId = 12345UL;

    private readonly Mock<IUserDisconnectionRepository> _repositoryMock = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly InitializationState _initializationState = new();

    /// <summary>DB上の user_disconnection_state を模した状態</summary>
    private readonly HashSet<ulong> _bannedUserIds = [];

    public BannedUserServiceTest()
    {
        _initializationState.MarkInitialized();

        _repositoryMock
            .Setup(r => r.GetBannedUserIdsAsync())
            .ReturnsAsync(() => _bannedUserIds.ToList());
        _repositoryMock
            .Setup(r => r.IsUserBannedAsync(It.IsAny<ulong>()))
            .ReturnsAsync((ulong userId) => _bannedUserIds.Contains(userId));
        _repositoryMock
            .Setup(r => r.BanUserAsync(It.IsAny<ulong>()))
            .Callback<ulong>(userId => _bannedUserIds.Add(userId))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.UnbanUserAsync(It.IsAny<ulong>()))
            .Callback<ulong>(userId => _bannedUserIds.Remove(userId))
            .Returns(Task.CompletedTask);
    }

    protected override void ConfigureTestServices(ServiceCollection services)
    {
        services.AddAllMocks();
        services.ReplaceMock(_repositoryMock);
        services.AddSingleton<ICacheGate>(
            _ => new CacheGate(_cache, new MutexRepository(), _initializationState));
        services.UseRealService<IBannedUserService, BannedUserService>();
    }

    public new void Dispose()
    {
        _cache.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "BANした直後からキャッシュ版が拒否を返すこと")]
    public async Task BanUserAsync_InvalidatesCache()
    {
        var service = GetService<IBannedUserService>();
        // 先にキャッシュを充填しておく
        Assert.False(await service.IsUserBannedCachedAsync(UserId));

        await service.BanUserAsync(UserId);

        Assert.True(await service.IsUserBannedCachedAsync(UserId));
    }

    [Fact(DisplayName = "UNBANした直後からキャッシュ版が拒否を返さないこと")]
    public async Task UnbanUserAsync_InvalidatesCache()
    {
        var service = GetService<IBannedUserService>();
        await service.BanUserAsync(UserId);
        Assert.True(await service.IsUserBannedCachedAsync(UserId));

        await service.UnbanUserAsync(UserId);

        Assert.False(await service.IsUserBannedCachedAsync(UserId));
    }

    [Fact(DisplayName = "キャッシュ版はuserIdごとにSELECTしないこと")]
    public async Task IsUserBannedCachedAsync_DoesNotQueryPerUserId()
    {
        var service = GetService<IBannedUserService>();

        await service.IsUserBannedCachedAsync(1UL);
        await service.IsUserBannedCachedAsync(2UL);
        await service.IsUserBannedCachedAsync(3UL);

        // 集合をまるごと1回読むだけ。userId単位のAnyAsyncは使わない
        _repositoryMock.Verify(r => r.GetBannedUserIdsAsync(), Times.Once);
        _repositoryMock.Verify(r => r.IsUserBannedAsync(It.IsAny<ulong>()), Times.Never);
    }

    [Fact(DisplayName = "非キャッシュ版はキャッシュを読まずに毎回DBを見ること")]
    public async Task IsUserBannedAsync_DoesNotUseCache()
    {
        // 司令卓など、ホットパス以外の経路は従来どおり最新のDBを見る
        var service = GetService<IBannedUserService>();
        Assert.False(await service.IsUserBannedCachedAsync(UserId));

        // キャッシュを無効化せずにDBだけを書き換える
        _bannedUserIds.Add(UserId);

        Assert.True(await service.IsUserBannedAsync(UserId));
        // キャッシュ版はまだ古い値を返す(=非キャッシュ版がキャッシュ経由でないことの裏返し)
        Assert.False(await service.IsUserBannedCachedAsync(UserId));
    }
}
