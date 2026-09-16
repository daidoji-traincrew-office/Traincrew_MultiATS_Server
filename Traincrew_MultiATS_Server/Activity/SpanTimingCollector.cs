using System.Collections.Concurrent;
using System.Diagnostics;

namespace Traincrew_MultiATS_Server.Activity;

public static class SpanTimingCollector
{
    private static readonly ConcurrentDictionary<string, Bucket> _buckets = new();
    private static readonly object _registerLock = new();
    private static ActivityListener? _listener;

    /// <summary>
    /// 1 スパン名あたりに保持するサンプル数の上限。
    /// 無制限に溜めると計測セッションが長いだけでメモリを食い潰すため頭を打たせる。
    /// (30 秒 × 14 列車 × 10 回/秒 でも 4200 件なので、通常の計測では上限 200,000 件に到達しない)
    /// </summary>
    private const int MaxSamplesPerBucket = 200_000;

    public sealed class Bucket
    {
        private readonly object _lock = new();
        private readonly List<double> _samples = new();

        public void Add(double ms)
        {
            lock (_lock)
            {
                if (_samples.Count >= MaxSamplesPerBucket)
                {
                    return;
                }

                _samples.Add(ms);
            }
        }

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

    /// <summary>
    /// 購読対象の ActivitySource 名。2 回目以降の <see cref="Register"/> でも
    /// 取りこぼさないよう、リスナーを作り直さずこの集合に追加していく。
    /// </summary>
    private static readonly HashSet<string> _sourceNames = [];

    public static void Register(params string[] sourceNames)
    {
        lock (_registerLock)
        {
            foreach (var sourceName in sourceNames)
            {
                _sourceNames.Add(sourceName);
            }

            if (_listener != null) return;
            RegisterCore();
        }
    }

    private static void RegisterCore()
    {
        _listener = new ActivityListener
        {
            // 後から Register された分も拾えるよう、判定時に集合を引く
            ShouldListenTo = src =>
            {
                lock (_registerLock) return _sourceNames.Contains(src.Name);
            },
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
