using Microsoft.Extensions.Caching.Memory;
using Traincrew_MultiATS_Server.Repositories.UserDisconnection;

namespace Traincrew_MultiATS_Server.Services;

public interface IBannedUserService
{
    Task<List<ulong>> GetBannedUserIdsAsync();
    Task<bool> IsUserBannedAsync(ulong userId);
    Task BanUserAsync(ulong userId);
    Task UnbanUserAsync(ulong userId);
}

public class BannedUserService(
    IUserDisconnectionRepository userDisconnectionRepository,
    IMemoryCache cache) : IBannedUserService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(10);

    private static string GetBanCacheKey(ulong userId) => $"ban:{userId}";

    public async Task<List<ulong>> GetBannedUserIdsAsync()
    {
        return await userDisconnectionRepository.GetBannedUserIdsAsync();
    }

    public async Task<bool> IsUserBannedAsync(ulong userId)
    {
        return (await cache.GetOrCreateAsync(GetBanCacheKey(userId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await userDisconnectionRepository.IsUserBannedAsync(userId);
        }))!;
    }

    public async Task BanUserAsync(ulong userId)
    {
        await userDisconnectionRepository.BanUserAsync(userId);
        cache.Remove(GetBanCacheKey(userId));
    }

    public async Task UnbanUserAsync(ulong userId)
    {
        await userDisconnectionRepository.UnbanUserAsync(userId);
        cache.Remove(GetBanCacheKey(userId));
    }
}
