using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Traincrew_MultiATS_Server.Repositories.Discord;

public class DiscordRepository(IConfiguration configuration) : IDiscordRepository
{
    private readonly DiscordSocketClient _client = new(new()
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildPresences
    });

    internal async Task Initialize()
    {
        var token = configuration.GetSection("Discord:BotToken").Get<string>();
        TaskCompletionSource tcs = new();
        _client.Ready += () =>
        {
            try
            {
                tcs.SetResult();
            }
            catch (InvalidOperationException){
                // ignored
            }
            return Task.CompletedTask;
        };
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        await tcs.Task;
    }

    internal async Task Logout()
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    public Task<SocketGuildUser> GetMember(ulong memberId)
    {
        var guild = _client.GetGuild(configuration.GetValue<ulong>("Discord:GuildId"));
        return Task.FromResult(guild.GetUser(memberId));
    }

    public async Task<RestGuildUser> GetMemberByToken(string token)
    {
        await using var client = new DiscordRestClient();
        await client.LoginAsync(TokenType.Bearer, token);
        
        return await client.GetCurrentUserGuildMemberAsync(configuration.GetValue<ulong>("Discord:GuildId")); 
    }
}