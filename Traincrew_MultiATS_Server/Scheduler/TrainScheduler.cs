using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class TrainScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 250;

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var trainHubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TrainHub, ITrainClientContract>>();
        var trainService = scope.ServiceProvider.GetRequiredService<TrainService>();

        var data = await trainService.CreateDataBySchedule();

        await trainHubContext.Clients.All.ReceiveData(data);
    }
}
