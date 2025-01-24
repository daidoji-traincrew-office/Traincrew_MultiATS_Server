using Discord.Rest;
using Traincrew_MultiATS_Server.Exception.DiscordAuthenticationException;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Discord;

namespace Traincrew_MultiATS_Server.Services;

public class DiscordService(IConfiguration configuration, IDiscordRepository discordRepository)
{
    public async Task<RestGuildUser> DiscordAuthentication(string token)
    {
        var beginnerRoleId = configuration.GetValue<ulong>("Discord:Roles:Beginner");
        var member =  await discordRepository.GetMemberByToken(token);
        // 運転会サーバーに所属しているか確認
        if (member is null)
        {
            throw new DiscordAuthenticationException("You are not a member of the specific server.");
        }

        // 最低限、教習生ロールを持っているか確認する
        if (!member.RoleIds.Contains(beginnerRoleId))
        {
            throw new DiscordAuthenticationException("You don't have the required role.");
        }

        return member;
    }

    public async Task<TraincrewRole> GetRoleByMemberId(ulong memberId)
    {
        var member = await discordRepository.GetMember(memberId);
        var roles = member.Roles.Select(role => role.Id).ToList();
        return GetRole(roles);
    }

    private TraincrewRole GetRole(IReadOnlyCollection<ulong> roleIds)
    {
        var roleIdSet = new HashSet<ulong>(roleIds);
        var result = new TraincrewRole
        {
            IsDriver = configuration.GetSection("Discord:Roles:Driver").Get<ulong[]>().Any(roleIdSet.Contains),
            IsCommander = configuration.GetSection("Discord:Roles:Commander").Get<ulong[]>().Any(roleIdSet.Contains),
            IsSignalman = configuration.GetSection("Discord:Roles:Signalman").Get<ulong[]>().Any(roleIdSet.Contains)
        };
        return result;
    }
}