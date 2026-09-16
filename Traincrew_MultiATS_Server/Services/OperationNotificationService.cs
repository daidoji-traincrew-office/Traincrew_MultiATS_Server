using Microsoft.Extensions.Caching.Memory;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.HostedService;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.OperationNotification;

namespace Traincrew_MultiATS_Server.Services;

public interface IOperationNotificationService
{
    Task<List<OperationNotificationData>> GetOperationNotificationData();
    Task<OperationNotificationData?> GetOperationNotificationDataByTrackCircuitIds(List<ulong> trackCircuitIds);
    Task SetOperationNotificationData(OperationNotificationData operationNotificationData);
    Task SetNoneWhereKaijoOrTorikeshiAndSpendMuchTime();
}

public class OperationNotificationService(
    IOperationNotificationRepository operationNotificationRepository,
    IGeneralRepository generalRepository,
    IDateTimeRepository dateTimeRepository,
    IMutexRepository mutexRepository,
    IMemoryCache cache,
    InitializationState initializationState) : IOperationNotificationService
{
    static readonly int kaijoTime = 20;

    /// <summary>
    /// 告知器と軌道回路の対応関係。初期化時に投入されるマスタデータで、以後変化しない。
    /// </summary>
    private const string CacheKeyTopology = "operationnotification:topology";

    /// <summary>
    /// 対応関係の保持期間。マスタデータが載せ替えられた場合に備えた保険としての TTL。
    /// </summary>
    private static readonly TimeSpan TopologyCacheTtl = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 告知器の状態。更新はすべて本サービス経由なので、書き込み時に明示的に無効化する。
    /// </summary>
    private const string CacheKeyStates = "operationnotification:states";

    /// <summary>
    /// 明示的な無効化が漏れた場合の保険としての TTL。
    /// </summary>
    private static readonly TimeSpan StateCacheTtl = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 告知器状態キャッシュの充填と無効化を直列化するためのミューテックスキー。
    /// <see cref="CacheKeyStates"/> と 1:1 に対応する。
    /// </summary>
    private const string MutexKeyStates = "operationnotification:states:mutex";

    /// <param name="TrackCircuitIdsByDisplayName">告知器名 -> 紐づく軌道回路 ID 集合</param>
    /// <param name="DisplayNameByTrackCircuitId">軌道回路 ID -> 告知器名</param>
    private sealed record Topology(
        Dictionary<string, HashSet<ulong>> TrackCircuitIdsByDisplayName,
        Dictionary<ulong, string> DisplayNameByTrackCircuitId);

    private async Task<Topology> GetTopology()
    {
        if (cache.TryGetValue(CacheKeyTopology, out Topology? cached))
        {
            return cached!;
        }

        var byDisplayName = await operationNotificationRepository.GetTrackCircuitIdsByDisplayName();
        // 逆引きは同じ対応関係から作れるので、DB へは 1 度しか問い合わせない
        var byTrackCircuitId = byDisplayName
            .SelectMany(kv => kv.Value.Select(trackCircuitId => (trackCircuitId, kv.Key)))
            .ToDictionary(x => x.trackCircuitId, x => x.Key);
        var topology = new Topology(
            byDisplayName.ToDictionary(kv => kv.Key, kv => kv.Value.ToHashSet()),
            byTrackCircuitId);

        // DB初期化が終わる前に呼ばれると、空または投入途中の不完全な対応関係を掴んでしまう。
        // それをキャッシュすると一部の告知器が TTL の間ずっと出なくなるため、
        // 初期化が完走するまではキャッシュに載せず、毎回DBを読む
        if (byTrackCircuitId.Count > 0 && initializationState.IsInitialized)
        {
            cache.Set(CacheKeyTopology, topology, TopologyCacheTtl);
        }

        return topology;
    }

    /// <summary>
    /// 全告知器の状態をキャッシュ経由で取得する。
    /// </summary>
    /// <remarks>
    /// キャッシュミス時の SELECT と <see cref="InvalidateStates"/> の Remove はミューテックスで
    /// 直列化する。GetOrCreateAsync だとファクトリ実行中に走った Remove は何も消さず、
    /// その後で陳腐化したスナップショットが TTL 分だけ格納されてしまい、
    /// 指令卓からの告知が最大 TTL の間運転士に見えなくなる。
    /// </remarks>
    private async Task<Dictionary<string, OperationNotificationState>> GetCachedStates()
    {
        // ヒット時はここで終わり(ミューテックスに触れない)
        if (cache.TryGetValue(CacheKeyStates, out Dictionary<string, OperationNotificationState>? cached))
        {
            return cached!;
        }

        await using var mutex = await mutexRepository.AcquireAsync(MutexKeyStates);
        // ゲート取得後に再チェック(同時ミス時に SELECT が並走するのを防ぐ)
        if (cache.TryGetValue(CacheKeyStates, out cached))
        {
            return cached!;
        }

        var states = await operationNotificationRepository.GetAllStates();
        var byDisplayName = states.ToDictionary(s => s.DisplayName);
        cache.Set(CacheKeyStates, byDisplayName, StateCacheTtl);
        return byDisplayName;
    }

    /// <summary>
    /// 全告知器の状態のキャッシュを破棄する。必ず DB 書き込みの完了後に呼ぶこと。
    /// </summary>
    private async Task InvalidateStates()
    {
        await using var mutex = await mutexRepository.AcquireAsync(MutexKeyStates);
        cache.Remove(CacheKeyStates);
    }

    public async Task<List<OperationNotificationData>> GetOperationNotificationData()
    {
        var displays = await operationNotificationRepository.GetAllDisplay();
        return displays.Select(ToOperationNotificationData).ToList();
    }

    public async Task<OperationNotificationData?> GetOperationNotificationDataByTrackCircuitIds(
        List<ulong> trackCircuitIds)
    {
        if (trackCircuitIds.Count == 0)
        {
            return null;
        }

        // 対応関係はマスタデータ、状態はキャッシュ済なので、この判定は DB アクセス無しで完結する
        var topology = await GetTopology();

        string? displayName = null;
        foreach (var trackCircuitId in trackCircuitIds)
        {
            if (!topology.DisplayNameByTrackCircuitId.TryGetValue(trackCircuitId, out var name))
            {
                // 運転告知器のない軌道回路があるならnullを返す
                return null;
            }

            if (displayName == null)
            {
                displayName = name;
            }
            else if (displayName != name)
            {
                // 複数の告知器にまたがっているならnullを返す
                return null;
            }
        }

        if (displayName == null
            || !topology.TrackCircuitIdsByDisplayName.TryGetValue(displayName, out var displayTrackCircuitIds)
            || !displayTrackCircuitIds.SetEquals(trackCircuitIds))
        {
            // まだホームトラックに入りきってない場合、nullを返す
            return null;
        }

        var states = await GetCachedStates();
        if (!states.TryGetValue(displayName, out var state))
        {
            return null;
        }

        return new()
        {
            DisplayName = displayName,
            Type = state.Type,
            Content = state.Content,
            OperatedAt = state.OperatedAt
        };
    }

    public async Task SetOperationNotificationData(OperationNotificationData operationNotificationData)
    {
        var state = new OperationNotificationState
        {
            DisplayName = operationNotificationData.DisplayName,
            Type = operationNotificationData.Type,
            Content = operationNotificationData.Content,
            OperatedAt = dateTimeRepository.GetNow()
        };

        await generalRepository.Save(state);
        await InvalidateStates();
    }

    public async Task SetNoneWhereKaijoOrTorikeshiAndSpendMuchTime()
    {
        var now = dateTimeRepository.GetNow();
        var operatedAt = now.AddSeconds(-kaijoTime);

        // 対象が 1 件も無い場合は UPDATE を発行しない(定期実行なので大半のティックで対象は無い)
        var states = await GetCachedStates();
        var hasTarget = states.Values.Any(s =>
            s.Type is OperationNotificationType.Kaijo or OperationNotificationType.Torikeshi
            && s.OperatedAt <= operatedAt);
        if (!hasTarget)
        {
            return;
        }

        await operationNotificationRepository.SetNoneWhereKaijoOrTorikeshiAndOperatedBeforeOrEqual(operatedAt);
        await InvalidateStates();
    }

    private static OperationNotificationData ToOperationNotificationData(
        OperationNotificationDisplay operationNotificationDisplay)
    {
        return new()
        {
            DisplayName = operationNotificationDisplay.Name,
            Type = operationNotificationDisplay.OperationNotificationState.Type,
            Content = operationNotificationDisplay.OperationNotificationState.Content,
            OperatedAt = operationNotificationDisplay.OperationNotificationState.OperatedAt
        };
    }
}