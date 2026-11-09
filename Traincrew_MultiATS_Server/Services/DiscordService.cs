using Traincrew_MultiATS_Server.Authentication;
using Traincrew_MultiATS_Server.Exception.DiscordAuthenticationException;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Discord;

namespace Traincrew_MultiATS_Server.Services;

public interface IDiscordService
{
    /// <summary>
    /// Discord ユーザー ID(アクセストークンの sub)から、運転会サーバーでのロールを返す。
    /// </summary>
    /// <exception cref="DiscordAuthenticationException">運転会サーバーのメンバーではない</exception>
    Task<TraincrewRole> GetRoleByMemberId(ulong? memberId);
}

public class DiscordService(
    IConfiguration configuration,
    IDiscordRepository discordRepository,
    EnableAuthorizationStore enableAuthorizationStore) : IDiscordService
{
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
                IsBeginner = true,
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
            IsBeginner = roleIdSet.Contains(configuration.GetValue<ulong>("Discord:Roles:Beginner")),
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