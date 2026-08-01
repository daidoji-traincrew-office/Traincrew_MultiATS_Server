using System.Collections.Concurrent;
using System.Diagnostics;

namespace Traincrew_MultiATS_Server.Activity;

public static class SpanTimingCollector
{
    private static readonly ConcurrentDictionary<string, Bucket> _buckets = new();
    private static ActivityListener? _listener;

    public sealed class Bucket
    {
        private readonly object _lock = new();
        private readonly List<double> _samples = new();
        public void Add(double ms) { lock (_lock) _samples.Add(ms); }
        public IReadOnlyList<double> Snapshot() { lock (_lock) return _samples.ToArray(); }
    }

    public sealed record Snapshot(int Count, double MeanMs, double P50Ms, double P95Ms, double P99Ms, double MaxMs, double TotalMs);

    public static void Reset() => _buckets.Clear();

    public static IReadOnlyDictionary<string, Snapshot> GetStats()
    {
        var result = new Dictionary<string, Snapshot>();
        foreach (var (name, bucket) in _buckets)
        {
            var samples = bucket.Snapshot();
            if (samples.Count == 0) continue;
            var sorted = samples.OrderBy(x => x).ToArray();
            double Percentile(double q)
            {
                var idx = Math.Min(sorted.Length - 1, (int)(q * sorted.Length));
                return sorted[idx];
            }
            result[name] = new Snapshot(
                Count: sorted.Length,
                MeanMs: sorted.Average(),
                P50Ms: Percentile(0.5),
                P95Ms: Percentile(0.95),
                P99Ms: Percentile(0.99),
                MaxMs: sorted[^1],
                TotalMs: sorted.Sum());
        }
        return result;
    }

    public static void Register(params string[] sourceNames)
    {
        if (_listener != null) return;
        var names = new HashSet<string>(sourceNames);
        _listener = new ActivityListener
        {
            ShouldListenTo = src => names.Contains(src.Name),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = act =>
            {
                var bucket = _buckets.GetOrAdd(act.OperationName, _ => new Bucket());
                bucket.Add(act.Duration.TotalMilliseconds);
            }
        };
        ActivitySource.AddActivityListener(_listener);
    }
}
