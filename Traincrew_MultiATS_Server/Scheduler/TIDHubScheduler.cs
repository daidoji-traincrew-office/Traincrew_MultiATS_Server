using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class TIDHubScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 333;

    protected override async Task ExecuteTaskAsync(IServiceScope scope)
    {
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TIDHub, ITIDClientContract>>();
        var tidService = scope.ServiceProvider.GetRequiredService<TIDService>();

        var data = await tidService.CreateTidData();

        await hubContext.Clients.All.ReceiveData(data);
    }
}
