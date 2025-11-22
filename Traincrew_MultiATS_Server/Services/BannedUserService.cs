using Traincrew_MultiATS_Server.Repositories.UserDisconnection;

namespace Traincrew_MultiATS_Server.Services;

public class BannedUserService(IUserDisconnectionRepository userDisconnectionRepository)
{
    public async Task<List<ulong>> GetBannedUserIdsAsync()
    {
        return await userDisconnectionRepository.GetBannedUserIdsAsync();
    }

    public async Task<bool> IsUserBannedAsync(ulong userId)
    {
        return await userDisconnectionRepository.IsUserBannedAsync(userId);
    }

    public async Task BanUserAsync(ulong userId)
    {
        await userDisconnectionRepository.BanUserAsync(userId);
    }

    public async Task UnbanUserAsync(ulong userId)
    {
        await userDisconnectionRepository.UnbanUserAsync(userId);
    }
}