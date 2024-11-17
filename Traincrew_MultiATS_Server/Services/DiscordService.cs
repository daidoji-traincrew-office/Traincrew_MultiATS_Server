using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Traincrew_MultiATS_Server.Exception.DiscordAuthenticationException;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Services;

public class DiscordService
{
    private readonly IConfiguration configuration;
    private readonly DiscordSocketClient client;
    private bool isStarted;
    private readonly TaskCompletionSource isReady = new();

    public DiscordService(IConfiguration configuration)
    {
        this.configuration = configuration;
        client = new DiscordSocketClient();
    }

    private async Task Initialize()
    {
        if (!isStarted)
        {
            var token = configuration.GetSection("Discord:BotToken").Get<string>();
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            isStarted = true;
        }
        client.Ready += () =>
        {
            isReady.SetResult();
            return Task.CompletedTask;
        };
        await isReady.Task;
    }

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

        return (member, GetRole(member.RoleIds));
    }

    public async Task<TraincrewRole> GetRoleByMemberId(ulong memberId)
    {
        // Todo: 本来はサーバー起動時にするべき
        await Initialize().WaitAsync(TimeSpan.FromSeconds(1));
        var guildId = configuration.GetValue<ulong>("Discord:GuildId");
        var guild = client.GetGuild(guildId);
        if (guild is null)
        {
            throw new DiscordAuthenticationException("The server is not found.");
        }
        var member = guild.GetUser(memberId);
        if (member is null)
        {
            throw new DiscordAuthenticationException("The member is not found.");
        } 
        return GetRole(member.Roles.Select(x => x.Id).ToArray());
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