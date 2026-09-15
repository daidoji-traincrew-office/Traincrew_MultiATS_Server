using Microsoft.Extensions.Caching.Memory;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Protection;

namespace Traincrew_MultiATS_Server.Services;

public interface IProtectionService
{
    Task<bool> IsProtectionEnabledForTrackCircuits(List<TrackCircuit> trackCircuits);
    Task UpdateBougoState(string trainNumber, List<TrackCircuit> trackCircuits, bool clientBougoState);
    Task<List<ProtectionRadioData>> GetProtectionRadioStates();
    Task AddProtectionZoneState(ProtectionRadioData data);
    Task UpdateProtectionZoneState(ProtectionRadioData data);
    Task DeleteProtectionZoneState(ulong id);
}

public class ProtectionService(
    IProtectionRepository protectionRepository,
    IGeneralRepository generalRepository,
    IMemoryCache cache) : IProtectionService
{
    /// <summary>
    /// 発報中の防護無線一覧のキャッシュキー。
    /// 発報数は多くても数件で、更新はすべて本サービス経由なので、全件をプロセス内に保持して
    /// ATS のホットパス(10 回/秒/列車)から DB アクセスを完全に無くす。
    /// </summary>
    private const string CacheKeyProtectionZoneStates = "protection:zonestates";

    /// <summary>
    /// 明示的な無効化が漏れた場合の保険としての TTL。
    /// </summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 発報中の防護無線一覧をキャッシュ経由で取得する。
    /// </summary>
    private async Task<List<ProtectionZoneState>> GetCachedProtectionZoneStates()
    {
        return (await cache.GetOrCreateAsync(CacheKeyProtectionZoneStates, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await protectionRepository.GetProtectionZoneStates();
        }))!;
    }

    private void InvalidateProtectionZoneStates()
    {
        cache.Remove(CacheKeyProtectionZoneStates);
    }

    public async Task<bool> IsProtectionEnabledForTrackCircuits(List<TrackCircuit> trackCircuits)
    {
        if (trackCircuits.Count == 0)
        {
            return false;
        }

        // 防護範囲の最大、最小を求め、それの+1、-1を求める
        var minProtectionZone = trackCircuits.Min(tc => tc.ProtectionZone) - 1;
        var maxProtectionZone = trackCircuits.Max(tc => tc.ProtectionZone) + 1;
        // その防護範囲で防護無線が発報されているか確認
        var states = await GetCachedProtectionZoneStates();
        return states.Any(s => minProtectionZone <= s.ProtectionZone && s.ProtectionZone <= maxProtectionZone);
    }

    private async Task EnableProtectionByTrackCircuits(string trainNumber, List<TrackCircuit> trackCircuits)
    {
        await protectionRepository.EnableProtection(
            trainNumber, trackCircuits.Select(tc => tc.ProtectionZone).ToList());
        InvalidateProtectionZoneStates();
    }

    private async Task DisableProtection(string trainNumber)
    {
        await protectionRepository.DisableProtection(trainNumber);
        InvalidateProtectionZoneStates();
    }

    public async Task UpdateBougoState(string trainNumber, List<TrackCircuit> trackCircuits, bool clientBougoState)
    {
        // 大半の呼び出しは「発報していない列車が発報していないまま」なので、
        // キャッシュ上の現状と一致していれば DB を触らない
        var states = await GetCachedProtectionZoneStates();
        var currentZones = states
            .Where(s => s.TrainNumber == trainNumber)
            .Select(s => s.ProtectionZone)
            .ToHashSet();

        if (clientBougoState)
        {
            var desiredZones = trackCircuits.Select(tc => tc.ProtectionZone).ToHashSet();
            if (currentZones.SetEquals(desiredZones))
            {
                return;
            }

            await EnableProtectionByTrackCircuits(trainNumber, trackCircuits);
        }
        else
        {
            if (currentZones.Count == 0)
            {
                return;
            }

            await DisableProtection(trainNumber);
        }
    }

    // ProtectionZoneStateの取得
    public async Task<List<ProtectionRadioData>> GetProtectionRadioStates()
    {
        var entities = await GetCachedProtectionZoneStates();
        return entities
            .Select(entity => new ProtectionRadioData
            {
                Id = entity.id,
                TrainNumber = entity.TrainNumber,
                ProtectionZone = entity.ProtectionZone
            })
            .ToList();
    }

    // ProtectionZoneStateの追加
    public async Task AddProtectionZoneState(ProtectionRadioData data)
    {
        await generalRepository.Add(new ProtectionZoneState
        {
            TrainNumber = data.TrainNumber,
            ProtectionZone = data.ProtectionZone
        });
        InvalidateProtectionZoneStates();
    }

    // ProtectionZoneStateの更新
    public async Task UpdateProtectionZoneState(ProtectionRadioData data)
    {
        await generalRepository.Save(new ProtectionZoneState
        {
            id = data.Id,
            TrainNumber = data.TrainNumber,
            ProtectionZone = data.ProtectionZone
        });
        InvalidateProtectionZoneStates();
    }

    // ProtectionZoneStateの削除
    public async Task DeleteProtectionZoneState(ulong id)
    {
        await protectionRepository.DeleteById(id);
        InvalidateProtectionZoneStates();
    }
}