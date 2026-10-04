using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class TIDHubScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 333;

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        // 全状態のスナップショットなので、詰まった接続に待たされないlatest-onlyで送る
        var latestOnlySender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<TIDHub>>();
        var tidService = scope.ServiceProvider.GetRequiredService<ITIDService>();

        var data = await tidService.CreateTidData();

        await latestOnlySender.SendAllLatestAsync(nameof(ITIDClientContract.ReceiveData), data);
    }
}
