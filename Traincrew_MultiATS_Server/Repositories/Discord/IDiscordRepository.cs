using Discord.Rest;
using Discord.WebSocket;

namespace Traincrew_MultiATS_Server.Repositories.Discord;

public interface IDiscordRepository
{
    Task Initialize();
    Task<SocketGuildUser> GetMember(ulong memberId);
    Task<RestGuildUser> GetMemberByToken(string token);
}