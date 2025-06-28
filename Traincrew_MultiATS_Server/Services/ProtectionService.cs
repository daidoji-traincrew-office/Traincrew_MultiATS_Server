using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.Protection;

namespace Traincrew_MultiATS_Server.Services;

public class ProtectionService(
    IProtectionRepository protectionRepository,
    IGeneralRepository generalRepository)
{
    public async Task<bool> IsProtectionEnabledForTrackCircuits(List<TrackCircuit> trackCircuits)
    {
        // 防護範囲の最大、最小を求め、それの+1、-1を求める
        var protectionZone = trackCircuits.Select(tc => tc.ProtectionZone).ToList();
        var minProtectionZone = protectionZone.Min() - 1;
        var maxProtectionZone = protectionZone.Max() + 1;
        // その防護範囲で防護無線が発報されているか確認
        return await protectionRepository.IsProtectionEnabled(minProtectionZone, maxProtectionZone);
    }

    private async Task EnableProtectionByTrackCircuits(string trainNumber, List<TrackCircuit> trackCircuits)
    {
        await protectionRepository.EnableProtection(
            trainNumber, trackCircuits.Select(tc => tc.ProtectionZone).ToList());
    }

    private async Task DisableProtection(string trainNumber)
    {
        await protectionRepository.DisableProtection(trainNumber);
    }

    public async Task UpdateBougoState(string trainNumber, List<TrackCircuit> trackCircuits, bool clientBougoState)
    {
        if (clientBougoState)
        {
            await EnableProtectionByTrackCircuits(trainNumber, trackCircuits);
        }
        else
        {
            await DisableProtection(trainNumber);
        }
    }
    
    // ProtectionZoneStateの取得
    public async Task<List<ProtectionZoneData>> GetProtectionZoneStates()
    {
        var entities = await protectionRepository.GetProtectionZoneStates();
        return entities
            .Select(entity => new ProtectionZoneData
            {
                Id = entity.id,
                TrainNumber = entity.TrainNumber,
                ProtectionZone = entity.ProtectionZone
            })
            .ToList();
    }

    // ProtectionZoneStateの追加
    public async Task AddProtectionZoneState(ProtectionZoneData data)
    {
        await generalRepository.Add(new ProtectionZoneState
        {
            TrainNumber = data.TrainNumber,
            ProtectionZone = data.ProtectionZone
        });
    }

    // ProtectionZoneStateの更新
    public async Task UpdateProtectionZoneState(ProtectionZoneData data)
    {
        await generalRepository.Save(new ProtectionZoneState
        {
            id = data.Id,
            TrainNumber = data.TrainNumber,
            ProtectionZone = data.ProtectionZone
        });
    }

    // ProtectionZoneStateの削除
    public async Task DeleteProtectionZoneState(ulong id)
    {
    }
}