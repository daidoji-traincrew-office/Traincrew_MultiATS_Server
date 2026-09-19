using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Protection;

public class ProtectionRepository(ApplicationDbContext context) : IProtectionRepository
{
    /// <summary>
    /// protection_zone_stateを全件取得する。
    /// </summary>
    /// <remarks>
    /// ATSのホットパスから毎tick呼ばれる。通常0行、発報中でも数行の極小テーブルなので、
    /// 絞り込みの述語を付けず(=パラメータ無しで文面を固定し)全件取る方が
    /// 文の本数とプランニングの両方で軽い。
    /// 追跡すると毎tickのエンティティがchange trackerに積まれるのでAsNoTrackingにする。
    /// </remarks>
    public async Task<List<ProtectionZoneState>> GetAll()
    {
        return await context.protectionZoneStates
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task Enable(string trainNumber, List<int> protectionZones)
    {
        // トランザクション内で処理する(追加削除は同時操作としたいため)
        await using var transaction = await context.Database.BeginTransactionAsync();
        // 既存のエントリを取得
        // Entityにしてるのは、削除時にEntityをそのまま渡すため
        // 何も考えずRemoveRangeにしてると、既存のTrackingしているEntityがいた時にエラーが出る
        var oldEntities = await context.protectionZoneStates
            .Where(x => x.TrainNumber == trainNumber)
            .ToListAsync();
        // 既存のProtectionZoneを取得
        var oldZones = oldEntities.Select(x => x.ProtectionZone).ToList();

        // 追加、削除するProtectionZoneを取得
        var zonesToAdd = protectionZones.Except(oldZones).ToList();
        var zonesToRemove = oldZones.Except(protectionZones).ToList();

        // 追加するProtectionZoneがあれば追加
        if (zonesToAdd.Count != 0)
        {
            context.protectionZoneStates.AddRange(zonesToAdd
                .Select(protectionZone => new ProtectionZoneState
                {
                    TrainNumber = trainNumber,
                    ProtectionZone = protectionZone
                }));
        }

        // 削除するProtectionZoneがあれば削除
        if (zonesToRemove.Count != 0)
        {
            context.protectionZoneStates.RemoveRange(
                oldEntities.Where(x => zonesToRemove.Contains(x.ProtectionZone))
            );
        }

        // 保存
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task Disable(string trainNumber)
    {
        await context.protectionZoneStates
            .Where(x => x.TrainNumber == trainNumber)
            .ExecuteDeleteAsync();
    }

    public async Task DeleteById(ulong id)
    {
        await context.protectionZoneStates
            .Where(x => x.id == id)
            .ExecuteDeleteAsync();
    }
}