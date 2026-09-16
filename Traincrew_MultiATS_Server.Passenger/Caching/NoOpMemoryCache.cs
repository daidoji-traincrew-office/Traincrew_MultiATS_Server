using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Traincrew_MultiATS_Server.Passenger.Caching;

/// <summary>
/// 何も保持しない <see cref="IMemoryCache"/> 実装。旅客用プロセスに登録する。
/// </summary>
/// <remarks>
/// 共有サービス層のキャッシュ(サーバーモード・時刻オフセット・BAN・告知器など)は、
/// 書き込みを行ったプロセス自身のキャッシュしか無効化できない。
/// 書き込み経路はすべて Crew 側にあるため、旅客用プロセスでキャッシュを持つと
/// 指令卓の操作が TTL 分だけ旅客に反映されない。
/// 旅客APIの呼び出し頻度は ATS と比べて圧倒的に少なく、毎回 DB を読んでも負荷にならないため、
/// このプロセスではキャッシュを一切持たずに常に DB の最新値を返す。
/// </remarks>
public sealed class NoOpMemoryCache : IMemoryCache
{
    public bool TryGetValue(object key, out object? value)
    {
        value = null;
        return false;
    }

    public ICacheEntry CreateEntry(object key)
    {
        return new NoOpCacheEntry(key);
    }

    public void Remove(object key)
    {
    }

    public void Dispose()
    {
    }

    /// <summary>
    /// Set した内容を捨てるだけのエントリ。IMemoryCache.Set 拡張メソッドが
    /// CreateEntry → Dispose の順で呼ぶため、格納先を持たないだけで十分。
    /// </summary>
    private sealed class NoOpCacheEntry(object key) : ICacheEntry
    {
        public object Key { get; } = key;
        public object? Value { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } =
            new List<PostEvictionCallbackRegistration>();
        public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;
        public long? Size { get; set; }

        public void Dispose()
        {
        }
    }
}
