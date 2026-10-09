using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class InterlockingHubScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 250;
    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        // 全状態のスナップショットなので、詰まった接続に待たされないlatest-onlyで送る
        var latestOnlySender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<InterlockingHub>>();
        var interlockingService = scope.ServiceProvider.GetRequiredService<IInterlockingService>();
        var data = await interlockingService.SendData_Interlocking();
        await latestOnlySender.SendAllLatestAsync(nameof(IInterlockingClientContract.ReceiveData), data);
    }
}