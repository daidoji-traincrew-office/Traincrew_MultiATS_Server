using Microsoft.Extensions.Caching.Memory;
using Moq;
using Traincrew_MultiATS_Server.Repositories.UserDisconnection;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Service;

/// <summary>
/// BannedUserService の IMemoryCache を使ったキャッシュ動作のテスト
/// </summary>
public class BannedUserServiceCacheTest
{
    private static (BannedUserService service, Mock<IUserDisconnectionRepository> repoMock) CreateService()
    {
        var repoMock = new Mock<IUserDisconnectionRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new BannedUserService(repoMock.Object, cache);
        return (service, repoMock);
    }

    [Fact]
    public async Task IsUserBannedAsync_CalledTwice_RepositoryCalledOnce()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.IsUserBannedAsync(It.IsAny<ulong>())).ReturnsAsync(true);

        var first = await service.IsUserBannedAsync(1);
        var second = await service.IsUserBannedAsync(1);

        Assert.True(first);
        Assert.True(second);
        repoMock.Verify(r => r.IsUserBannedAsync(1), Times.Once);
    }

    [Fact]
    public async Task BanUserAsync_InvalidatesCache_RepositoryCalledAgain()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.IsUserBannedAsync(It.IsAny<ulong>())).ReturnsAsync(false);

        _ = await service.IsUserBannedAsync(1);
        await service.BanUserAsync(1);
        _ = await service.IsUserBannedAsync(1);

        repoMock.Verify(r => r.IsUserBannedAsync(1), Times.Exactly(2));
    }

    [Fact]
    public async Task UnbanUserAsync_InvalidatesCache_RepositoryCalledAgain()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.IsUserBannedAsync(It.IsAny<ulong>())).ReturnsAsync(true);

        _ = await service.IsUserBannedAsync(2);
        await service.UnbanUserAsync(2);
        _ = await service.IsUserBannedAsync(2);

        repoMock.Verify(r => r.IsUserBannedAsync(2), Times.Exactly(2));
    }

    [Fact]
    public async Task IsUserBannedAsync_DifferentUsers_RepositoryCalledForEach()
    {
        var (service, repoMock) = CreateService();
        repoMock.Setup(r => r.IsUserBannedAsync(It.IsAny<ulong>())).ReturnsAsync(false);

        _ = await service.IsUserBannedAsync(1);
        _ = await service.IsUserBannedAsync(2);

        repoMock.Verify(r => r.IsUserBannedAsync(1), Times.Once);
        repoMock.Verify(r => r.IsUserBannedAsync(2), Times.Once);
    }
}
