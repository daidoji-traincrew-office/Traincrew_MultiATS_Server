using Microsoft.Extensions.Caching.Memory;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.General;
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
    IMemoryCache cache) : IOperationNotificationService
{
    static readonly int kaijoTime = 20;

    /// <summary>
    /// 告知器と軌道回路の対応関係。初期化時に投入されるマスタデータで、以後変化しない。
    /// </summary>
    private const string CacheKeyTopology = "operationnotification:topology";

    /// <summary>
    /// 告知器の状態。更新はすべて本サービス経由なので、書き込み時に明示的に無効化する。
    /// </summary>
    private const string CacheKeyStates = "operationnotification:states";

    /// <summary>
    /// 明示的な無効化が漏れた場合の保険としての TTL。
    /// </summary>
    private static readonly TimeSpan StateCacheTtl = TimeSpan.FromSeconds(10);

    /// <param name="TrackCircuitIdsByDisplayName">告知器名 -> 紐づく軌道回路 ID 集合</param>
    /// <param name="DisplayNameByTrackCircuitId">軌道回路 ID -> 告知器名</param>
    private sealed record Topology(
        Dictionary<string, HashSet<ulong>> TrackCircuitIdsByDisplayName,
        Dictionary<ulong, string> DisplayNameByTrackCircuitId);

    private async Task<Topology> GetTopology()
    {
        return (await cache.GetOrCreateAsync(CacheKeyTopology, async _ =>
        {
            var byDisplayName = await operationNotificationRepository.GetTrackCircuitIdsByDisplayName();
            var byTrackCircuitId = await operationNotificationRepository.GetDisplayNameByTrackCircuitId();
            return new Topology(
                byDisplayName.ToDictionary(kv => kv.Key, kv => kv.Value.ToHashSet()),
                byTrackCircuitId);
        }))!;
    }

    private async Task<Dictionary<string, OperationNotificationState>> GetCachedStates()
    {
        return (await cache.GetOrCreateAsync(CacheKeyStates, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = StateCacheTtl;
            var states = await operationNotificationRepository.GetAllStates();
            return states.ToDictionary(s => s.DisplayName);
        }))!;
    }

    private void InvalidateStates()
    {
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
        InvalidateStates();
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
        InvalidateStates();
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