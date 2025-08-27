using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

// このSchedulerは常時実行する
public class ServerModeScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 250;

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<CommanderTableHub, ICommanderTableClientContract>>();
        var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();

        var serverMode = await serverService.GetServerModeAsync();

        await hubContext.Clients.All.ReceiveServerMode(serverMode);
    }
}