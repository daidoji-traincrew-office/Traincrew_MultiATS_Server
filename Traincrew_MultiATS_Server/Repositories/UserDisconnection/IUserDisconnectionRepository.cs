namespace Traincrew_MultiATS_Server.Repositories.UserDisconnection;

public interface IUserDisconnectionRepository
{
    Task<List<ulong>> GetBannedUserIdsAsync();
    Task<bool> IsUserBannedAsync(ulong userId);
    Task BanUserAsync(ulong userId);
    Task UnbanUserAsync(ulong userId);
}