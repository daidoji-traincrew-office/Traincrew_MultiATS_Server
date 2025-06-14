using Discord.Rest;
using Traincrew_MultiATS_Server.Authentication;
using Traincrew_MultiATS_Server.Exception.DiscordAuthenticationException;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Discord;

namespace Traincrew_MultiATS_Server.Services;

public class DiscordService(
    IConfiguration configuration,
    IDiscordRepository discordRepository,
    EnableAuthorizationStore enableAuthorizationStore)
{
    public async Task<RestGuildUser> DiscordAuthentication(string token)
    {
        var member =  await discordRepository.GetMemberByToken(token);
        return member;
    }

    public async Task<bool> IsUserCanAuthenticate(ulong userId)
    {
        var beginnerRoleId = configuration.GetValue<ulong>("Discord:Roles:Beginner");
        var member =  await discordRepository.GetMember(userId);
        // 運転会サーバーに所属しているか、入鋏ロールを持っているか確認
        return member is not null && member.Roles.Any(x => x.Id == beginnerRoleId);
    }

    public async Task<TraincrewRole> GetRoleByMemberId(ulong? memberId)
    {
        if (memberId == null)
        {
            // 認証が有効になっており、memberIdがnullの場合はエラーを返す
            if (enableAuthorizationStore.EnableAuthorization)
            {
                throw new InvalidOperationException("Authorization is enabled, But memberId is null.");
            }

            // ローカルデバッグ用のTraincrewRoleを返す(全許可)
            return new()
            {
                IsDriver = true,
                IsDriverManager = true,
                IsConductor = true,
                IsCommander = true,
                IsSignalman = true,
                IsAdministrator = true
            };
        }
        var member = await discordRepository.GetMember(memberId.Value);
        if (member is null)
        {
            throw new DiscordAuthenticationException($"Member ID: {memberId} not found.");
        }
        var roles = member.Roles.Select(role => role.Id).ToList();
        return GetRole(roles);
    }

    private TraincrewRole GetRole(IReadOnlyCollection<ulong> roleIds)
    {
        var roleIdSet = new HashSet<ulong>(roleIds);
        var result = new TraincrewRole
        {
            IsDriver = configuration.GetSection("Discord:Roles:Driver").Get<ulong[]>().Any(roleIdSet.Contains),
            IsDriverManager = configuration.GetSection("Discord:Roles:DriverManager").Get<ulong[]>().Any(roleIdSet.Contains),
            IsConductor = configuration.GetSection("Discord:Roles:Conductor").Get<ulong[]>().Any(roleIdSet.Contains),
            IsCommander = configuration.GetSection("Discord:Roles:Commander").Get<ulong[]>().Any(roleIdSet.Contains),
            IsSignalman = configuration.GetSection("Discord:Roles:Signalman").Get<ulong[]>().Any(roleIdSet.Contains),
            IsAdministrator = configuration.GetSection("Discord:Roles:Administrator").Get<ulong[]>().Any(roleIdSet.Contains),
        };
        return result;
    }
}