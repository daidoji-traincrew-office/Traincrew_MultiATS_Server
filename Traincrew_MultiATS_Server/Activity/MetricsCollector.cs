using System.Diagnostics.Metrics;

namespace Traincrew_MultiATS_Server.Activity;

public class MetricsCollector
{
    private int _cachedTrainCount;
    private int _cachedServerMode = -1;

#pragma warning disable IDE0052
    private readonly ObservableGauge<int> _trainCountGauge;
    private readonly ObservableGauge<int> _serverModeGauge;
#pragma warning restore IDE0052

    public MetricsCollector()
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

    public void UpdateTrainCount(int count) => Interlocked.Exchange(ref _cachedTrainCount, count);
    public void UpdateServerMode(int mode) => Interlocked.Exchange(ref _cachedServerMode, mode);
}
