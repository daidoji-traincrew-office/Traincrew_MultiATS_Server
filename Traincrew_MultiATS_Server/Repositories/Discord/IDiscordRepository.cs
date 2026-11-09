using Discord.WebSocket;

namespace Traincrew_MultiATS_Server.Repositories.Discord;

public interface IDiscordRepository
{
    Task<SocketGuildUser?> GetMember(ulong memberId);
}