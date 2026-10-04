using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class CTCPHubScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 500;

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        // 全状態のスナップショットなので、詰まった接続に待たされないlatest-onlyで送る
        var latestOnlySender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<CTCPHub>>();
        var ctcpService = scope.ServiceProvider.GetRequiredService<ICTCPService>();

        var data = await ctcpService.SendData_CTCP();

        await latestOnlySender.SendAllAsync(nameof(ICTCPClientContract.ReceiveData), data);
    }
}
