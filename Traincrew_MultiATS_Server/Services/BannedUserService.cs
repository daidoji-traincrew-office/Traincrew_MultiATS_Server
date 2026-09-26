using Traincrew_MultiATS_Server.Repositories.UserDisconnection;
using Traincrew_MultiATS_Server.Services.Cache;

namespace Traincrew_MultiATS_Server.Services;

public interface IBannedUserService
{
    Task<List<ulong>> GetBannedUserIdsAsync();
    Task<bool> IsUserBannedAsync(ulong userId);
    Task<bool> IsUserBannedCachedAsync(ulong userId);
    Task BanUserAsync(ulong userId);
    Task UnbanUserAsync(ulong userId);
}

public class BannedUserService(
    IUserDisconnectionRepository userDisconnectionRepository,
    ICacheGate cacheGate) : IBannedUserService
{
    public async Task<List<ulong>> GetBannedUserIdsAsync()
    {
        return await userDisconnectionRepository.GetBannedUserIdsAsync();
    }

    public async Task<bool> IsUserBannedAsync(ulong userId)
    {
        return await userDisconnectionRepository.IsUserBannedAsync(userId);
    }

    /// <summary>
    /// 接続拒否されているかを、プロセス内にキャッシュした集合から判定する。
    /// </summary>
    /// <remarks>
    /// ATSのホットパスから毎tick呼ばれるため、userIdごとのSELECTを避ける。
    /// userId単位ではキャッシュしない。「拒否されていない」の否定キャッシュが
    /// 無限に増え、無効化も漏れるため、拒否済みidの集合をまるごと1エントリで持つ。
    /// user_disconnection_state を書くのは司令卓のBAN/UNBANだけ(いずれもCrewプロセス内)で、
    /// どちらもこのキャッシュを無効化する。
    /// </remarks>
    public async Task<bool> IsUserBannedCachedAsync(ulong userId)
    {
        var bannedUserIds = await cacheGate.GetOrFillAsync<IReadOnlySet<ulong>>(
            CacheKeys.BannedUserIds,
            async () => (await userDisconnectionRepository.GetBannedUserIdsAsync()).ToHashSet());
        return bannedUserIds.Contains(userId);
    }

    public async Task BanUserAsync(ulong userId)
    {
        await userDisconnectionRepository.BanUserAsync(userId);
        await cacheGate.InvalidateAsync(CacheKeys.BannedUserIds);
    }

    public async Task UnbanUserAsync(ulong userId)
    {
        await userDisconnectionRepository.UnbanUserAsync(userId);
        await cacheGate.InvalidateAsync(CacheKeys.BannedUserIds);
    }
}
