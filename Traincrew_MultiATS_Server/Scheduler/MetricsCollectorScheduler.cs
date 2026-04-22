using Traincrew_MultiATS_Server.Activity;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Repositories.Train;

namespace Traincrew_MultiATS_Server.Scheduler;

public class MetricsCollectorScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 10000; // 10秒

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var metrics = scope.ServiceProvider.GetRequiredService<MetricsCollector>();
        var trainRepository = scope.ServiceProvider.GetRequiredService<ITrainRepository>();
        var serverRepository = scope.ServiceProvider.GetRequiredService<IServerRepository>();

        var trainCount = await trainRepository.GetCount();
        metrics.UpdateTrainCount(trainCount);
        activity?.SetTag("train_count", trainCount);

        var serverState = await serverRepository.GetServerStateAsync();
        var serverMode = serverState?.Mode switch
        {
            ServerMode.Off => 0,
            ServerMode.Private => 1,
            ServerMode.Public => 2,
            _ => -1
        };
        metrics.UpdateServerMode(serverMode);
        activity?.SetTag("server_mode", serverMode);
    }
}
