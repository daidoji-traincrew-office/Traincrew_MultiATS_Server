using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class InterlockingHubScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 100;
    protected override async Task ExecuteTaskAsync(IServiceScope scope)
    {
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<InterlockingHub, IInterlockingClientContract>>();
        var interlockingService = scope.ServiceProvider.GetRequiredService<InterlockingService>();
        var data = await interlockingService.SendData_Interlocking();
        await hubContext.Clients.All.ReceiveData(data);
    }
}