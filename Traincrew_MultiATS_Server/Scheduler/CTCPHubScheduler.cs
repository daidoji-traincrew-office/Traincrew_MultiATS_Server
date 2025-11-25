using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class CTCPHubScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 500;

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<CTCPHub, ICTCPClientContract>>();
        var ctcpService = scope.ServiceProvider.GetRequiredService<CTCPService>();

        var data = await ctcpService.SendData_CTCP();

        await hubContext.Clients.All.ReceiveData(data);
    }
}
