using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class CommanderTableHubScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 250;

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        // 全状態のスナップショットなので、詰まった接続に待たされないlatest-onlyで送る
        var latestOnlySender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<CommanderTableHub>>();
        var commanderTableService = scope.ServiceProvider.GetRequiredService<ICommanderTableService>();

        var data = await commanderTableService.SendData_CommanderTable();

        await latestOnlySender.SendAllLatestAsync(nameof(ICommanderTableClientContract.ReceiveData), data);
    }
}