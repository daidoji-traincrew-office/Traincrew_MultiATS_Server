using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteConflict;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

public class RouteConflictDbInitializer(
    ILogger<RouteConflictDbInitializer> logger,
    IRouteRepository routeRepository,
    ILockConditionRepository lockConditionRepository,
    ISwitchingMachineRouteRepository switchingMachineRouteRepository,
    IRouteConflictRepository routeConflictRepository,
    IGeneralRepository generalRepository
)
{
    public async Task InitializeRouteConflictAsync(CancellationToken cancellationToken)
    {
        // 登録済みの競合進路
        var existingRouteConflicts = await routeConflictRepository.GetAll(cancellationToken);
        var existingPairs = existingRouteConflicts
            .Select(r => (r.RouteId, r.ConflictingRouteId))
            .ToHashSet();
        // 全進路のID
        var routeIds = await routeRepository.GetIdsForAll();
        var routeIdSet = routeIds.ToHashSet();
        // 進路ID → 鎖錠欄の条件(進路同士の直接鎖錠用)
        var lockConditionsByRouteId = await lockConditionRepository
            .GetConditionsByObjectIdsAndType(routeIds, LockType.Lock);
        // 転てつ器進路(転てつ器によって競合しているもの用)
        var switchingMachineRoutes = await switchingMachineRouteRepository.GetAll();
        // 進路ID -> 転てつ器IDと定位/反位の組のリスト
        var switchingMachineRouteIdsByRouteId = switchingMachineRoutes
            .GroupBy(r => r.RouteId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => (r.SwitchingMachineId, r.IsReverse)
                ).ToList());
        // (転てつ器ID, 定位/反位) -> 進路IDのリスト
        var routeIdsBySwitchingMachineIdAndIsReversed = switchingMachineRoutes
            .GroupBy(r => (r.SwitchingMachineId, r.IsReverse))
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => r.RouteId
                ).ToList());

        // 競合している進路の組(重複排除のため HashSet)
        var conflictPairs = new HashSet<(ulong RouteId, ulong ConflictingRouteId)>();

        // 各進路でループ
        foreach (var routeId in routeIds)
        {
            // まず直接鎖錠している進路同士を直接セットへ(鎖錠先が進路であることを確かめる)
            // TODO: 条件付き鎖錠(Or/Not の子)の中の進路も区別せず競合とみなしている。現状そういう進路はない想定
            var conflictingRouteIdsByRoute = lockConditionsByRouteId
                .GetValueOrDefault(routeId, [])
                .OfType<LockConditionObject>()
                .Where(lco => routeIdSet.Contains(lco.ObjectId))
                .Select(lco => lco.ObjectId)
                .Where(id => id != routeId)
                .ToHashSet();
            // 次に自進路によって鎖錠している転てつ器と逆方向に鎖錠をする進路をセットにいれる
            var switchingMachineRoute = switchingMachineRouteIdsByRouteId
                .GetValueOrDefault(routeId, []);
            var conflictingRouteIdsBySwitchingMachine = switchingMachineRoute
                .SelectMany(t => routeIdsBySwitchingMachineIdAndIsReversed
                    .GetValueOrDefault((t.SwitchingMachineId, t.IsReverse == NR.Normal ? NR.Reversed : NR.Normal), [])
                )
                .ToHashSet();
            // そのセットをもとに、競合している進路をセットにいれる。
            var conflictingRouteIds = conflictingRouteIdsByRoute
                .Concat(conflictingRouteIdsBySwitchingMachine)
                .ToHashSet();
            foreach (var conflictingRouteId in conflictingRouteIds)
            {
                conflictPairs.Add((routeId, conflictingRouteId));
                conflictPairs.Add((conflictingRouteId, routeId));
            }
        }

        // 最後に、競合しなくなった組を削除
        var routeConflictsToDelete = existingRouteConflicts
            .Where(r => !conflictPairs.Contains((r.RouteId, r.ConflictingRouteId)))
            .ToList();
        if (routeConflictsToDelete.Count > 0)
        {
            await generalRepository.DeleteAll(routeConflictsToDelete, cancellationToken);
            logger.LogInformation("競合進路を {Count} 件削除しました", routeConflictsToDelete.Count);
        }

        // 新たに競合する組を登録する
        var routeConflictsToAdd = conflictPairs
            .Where(pair => !existingPairs.Contains(pair))
            .Select(pair => new RouteConflict
            {
                RouteId = pair.RouteId,
                ConflictingRouteId = pair.ConflictingRouteId
            })
            .ToList();
        if (routeConflictsToAdd.Count > 0)
        {
            await generalRepository.AddAll(routeConflictsToAdd, cancellationToken);
            logger.LogInformation("競合進路を {Count} 件登録しました", routeConflictsToAdd.Count);
        }
    }
}