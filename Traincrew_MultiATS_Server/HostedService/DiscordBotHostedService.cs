using Traincrew_MultiATS_Server.Repositories.Discord;

namespace Traincrew_MultiATS_Server.HostedService;

public class DiscordBotHostedService(DiscordRepository discordRepository) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await discordRepository.Initialize();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await discordRepository.Logout();
    }
}