using Microsoft.Extensions.Caching.Memory;
using Traincrew_MultiATS_Server.HostedService;
using Traincrew_MultiATS_Server.Repositories.Mutex;

namespace Traincrew_MultiATS_Server.Services.Cache;

/// <summary>
/// 単一エントリのキャッシュを、確実に無効化できる形で読み書きする。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IMemoryCache"/> の GetOrCreateAsync は使ってはならない。
/// ファクトリの実行中はエントリが確定しないため、SELECTが飛んでいる最中に着弾した
/// Remove が no-op になり、書き込み前のスナップショットがTTLいっぱい居座る。
/// ここでは TryGetValue → mutex → 二重チェック → Set に統一している。
/// </para>
/// <para>
/// 呼ぶ側が守ること:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <c>factory</c> にサービスのmutexを取るメソッドを渡さない。ロック順は
/// 「サービスのmutex → cache:* のmutex」の一方向だけにする。例えば
/// ServerService.SetServerModeAsync は nameof(ServerService) を保持したまま
/// InvalidateAsync を呼ぶので、factory側が同じキーを取ると自己デッドロックする
/// (IMutexRepository は再入できない)。渡すのは ...WithoutLock 系だけ。
/// </description></item>
/// <item><description>
/// 載せるのは不変スナップショットだけにする。EFのエンティティを載せない。
/// コレクションは IReadOnlySet / IReadOnlyDictionary で配る。
/// </description></item>
/// <item><description>
/// 無効化はDB書き込みの完了後に行う。書いた値をSetし直す書き戻しはしない
/// (DB側の既定値変換やトリガで実値がずれたときにキャッシュだけ嘘をつく)。
/// </description></item>
/// <item><description>
/// TTLは保険であって主対策ではない。主対策は無効化が確実に効くこと。
/// 項目ごとにTTLを変えたくなったら設計が間違っている合図。
/// </description></item>
/// </list>
/// </remarks>
public interface ICacheGate
{
    /// <summary>
    /// キャッシュから読む。無ければ <paramref name="factory"/> で埋める。
    /// 同一キーに同時に来た呼び出しのうち、factoryを走らせるのは1つだけ。
    /// </summary>
    Task<T> GetOrFillAsync<T>(string key, Func<Task<T>> factory, CancellationToken ct = default);

    /// <summary>
    /// キャッシュを捨てる。DB書き込みの完了後に呼ぶこと。
    /// </summary>
    Task InvalidateAsync(string key, CancellationToken ct = default);
}

/// <inheritdoc cref="ICacheGate"/>
public class CacheGate(
    IMemoryCache cache,
    IMutexRepository mutexRepository,
    InitializationState initializationState) : ICacheGate
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(10);

    public async Task<T> GetOrFillAsync<T>(string key, Func<Task<T>> factory, CancellationToken ct = default)
    {
        // ヒット時はmutexに触れない(ATS tickのホットパス)
        if (cache.TryGetValue(key, out T? hit))
        {
            return hit!;
        }

        await using var mutex = await mutexRepository.AcquireAsync(MutexKey(key), ct);
        // 二重チェック。待っている間に他の呼び出しが埋めているかもしれない
        if (cache.TryGetValue(key, out hit))
        {
            return hit!;
        }

        var value = await factory();
        if (initializationState.IsInitialized)
        {
            // 初期化が完走する前は載せない。途中の不完全な状態がTTL分居座るのを防ぐ。
            // Passengerは MarkInitialized を呼ぶHostedServiceを持たないので、
            // ここが常にfalseになり構造的にキャッシュを持たない。
            cache.Set(key, value, Ttl);
        }

        return value;
    }

    public async Task InvalidateAsync(string key, CancellationToken ct = default)
    {
        // 充填中の Remove が取りこぼされないよう、充填と同じmutexの下で消す
        await using var mutex = await mutexRepository.AcquireAsync(MutexKey(key), ct);
        cache.Remove(key);
    }

    private static string MutexKey(string key) => $"cache:{key}";
}
