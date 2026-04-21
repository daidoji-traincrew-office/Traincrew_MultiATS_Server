using System.Diagnostics.Metrics;
using Traincrew_MultiATS_Server.Activity;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Server;
using Traincrew_MultiATS_Server.Repositories.Train;

namespace Traincrew_MultiATS_Server.Scheduler;

public class MetricsCollectorScheduler : Scheduler
{
    private readonly ObservableGauge<int> _trainCountGauge;
    private readonly ObservableGauge<int> _serverModeGauge;
    private int _cachedTrainCount;
    private int _cachedServerMode = -1;

    public MetricsCollectorScheduler(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
    {
        _trainCountGauge = ApplicationMetrics.Meter.CreateObservableGauge(
            "train_count",
            () => Interlocked.CompareExchange(ref _cachedTrainCount, 0, 0),
            unit: "trains",
            description: "Current number of trains");

        _serverModeGauge = ApplicationMetrics.Meter.CreateObservableGauge(
            "server_mode",
            () => Interlocked.CompareExchange(ref _cachedServerMode, 0, 0),
            unit: "mode",
            description: "Current server mode (0=Off, 1=Private, 2=Public)");
    }

    protected override int Interval => 10000; // 10秒

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var trainRepository = scope.ServiceProvider.GetRequiredService<ITrainRepository>();
        var serverRepository = scope.ServiceProvider.GetRequiredService<IServerRepository>();

        // 列車数を更新
        var trainCount = await trainRepository.GetCount();
        Interlocked.Exchange(ref _cachedTrainCount, trainCount);
        activity?.SetTag("train_count", trainCount);

        // サーバーモードを更新
        var serverState = await serverRepository.GetServerStateAsync();
        var serverMode = serverState?.Mode switch
        {
            ServerMode.Off => 0,
            ServerMode.Private => 1,
            ServerMode.Public => 2,
            _ => -1
        };
        Interlocked.Exchange(ref _cachedServerMode, serverMode);
        activity?.SetTag("server_mode", serverMode);
    }
}
