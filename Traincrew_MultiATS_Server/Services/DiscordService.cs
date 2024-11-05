using Discord;
using Discord.Rest;
using Traincrew_MultiATS_Server.Exception.DiscordAuthenticationException;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Services;

public class DiscordService(IConfiguration configuration)
{
    public async Task<(RestGuildUser, TraincrewRole)> DiscordAuthentication(string token)
    {
        var guildId = configuration.GetValue<ulong>("Discord:GuildId");
        var beginnerRoleId = configuration.GetValue<ulong>("Discord:Roles:Beginner");
        await using var client = new DiscordRestClient();
        await client.LoginAsync(TokenType.Bearer, token);

        // 運転会サーバーに所属しているか確認
        var member = await client.GetCurrentUserGuildMemberAsync(guildId);
        if (member is null)
        {
            throw new DiscordAuthenticationException("You are not a member of the specific server.");
        }

        // 最低限、教習生ロールを持っているか確認する
        if (!member.RoleIds.Contains(beginnerRoleId))
        {
            throw new DiscordAuthenticationException("You don't have the required role.");
        }

        return (member, GetRole(member));
    }

    private TraincrewRole GetRole(RestGuildUser member)
    {
        var roleIds = new HashSet<ulong>(member.RoleIds);
        var result = new TraincrewRole
        {
            IsDriver = configuration.GetSection("Discord:Roles:Driver").Get<ulong[]>().Any(roleIds.Contains),
            IsCommander = configuration.GetSection("Discord:Roles:Commander").Get<ulong[]>().Any(roleIds.Contains),
            IsSignalman = configuration.GetSection("Discord:Roles:Signalman").Get<ulong[]>().Any(roleIds.Contains)
        };
        return result;
    }
}