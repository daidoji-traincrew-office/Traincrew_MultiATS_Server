using Microsoft.Extensions.Caching.Memory;
using Traincrew_MultiATS_Server.Repositories.Mutex;
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
    IMutexRepository mutexRepository,
    IMemoryCache cache) : IBannedUserService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 接続拒否判定キャッシュの充填と無効化を直列化するためのミューテックスキー。
    /// ユーザ単位に分けてもよいが、キーを増やすほどミューテックスが溜まるので全ユーザ共通にする。
    /// </summary>
    private const string MutexKeyBan = "ban:mutex";

    private static string GetBanCacheKey(ulong userId) => $"ban:{userId}";

    public async Task<List<ulong>> GetBannedUserIdsAsync()
    {
        return await userDisconnectionRepository.GetBannedUserIdsAsync();
    }

    public async Task<bool> IsUserBannedAsync(ulong userId)
    {
        var cacheKey = GetBanCacheKey(userId);
        // ヒット時はここで終わり(ミューテックスに触れない)
        if (cache.TryGetValue(cacheKey, out bool cached))
        {
            return cached;
        }

        // GetOrCreateAsync だとファクトリ実行中に走った Remove が何も消さず、
        // その後で陳腐化した値が TTL 分だけ格納される(BAN が最大 TTL の間効かない)ため直列化する
        await using var mutex = await mutexRepository.AcquireAsync(MutexKeyBan);
        if (cache.TryGetValue(cacheKey, out cached))
        {
            return cached;
        }

        var isBanned = await userDisconnectionRepository.IsUserBannedAsync(userId);
        cache.Set(cacheKey, isBanned, CacheTtl);
        return isBanned;
    }

    public async Task BanUserAsync(ulong userId)
    {
        await userDisconnectionRepository.BanUserAsync(userId);
        await InvalidateBanCache(userId);
    }

    public async Task UnbanUserAsync(ulong userId)
    {
        await userDisconnectionRepository.UnbanUserAsync(userId);
        await InvalidateBanCache(userId);
    }

    /// <summary>
    /// 接続拒否判定のキャッシュを破棄する。必ず DB 書き込みの完了後に呼ぶこと。
    /// </summary>
    private async Task InvalidateBanCache(ulong userId)
    {
        await using var mutex = await mutexRepository.AcquireAsync(MutexKeyBan);
        cache.Remove(GetBanCacheKey(userId));
    }
}
