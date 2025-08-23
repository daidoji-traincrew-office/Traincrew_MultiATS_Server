using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class CommanderTableHubScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 250;

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<CommanderTableHub, ICommanderTableClientContract>>();
        var commanderTableService = scope.ServiceProvider.GetRequiredService<CommanderTableService>();

        var data = await commanderTableService.SendData_CommanderTable();

        await hubContext.Clients.All.ReceiveData(data);
    }
}