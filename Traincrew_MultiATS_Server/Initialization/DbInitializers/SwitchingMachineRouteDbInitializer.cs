using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.LockCondition;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes switching machine route relationships and sets station IDs for interlocking objects
/// </summary>
public partial class SwitchingMachineRouteDbInitializer(
    ApplicationDbContext context,
    ILockConditionRepository lockConditionRepository,
    ILogger<SwitchingMachineRouteDbInitializer> logger)
    : BaseDbInitializer(context, logger)
{
    [GeneratedRegex(@"^(TH(\d{1,2}S?))_")]
    private static partial Regex RegexStationId();

    /// <summary>
    ///     Set station ID to interlocking objects based on naming pattern
    /// </summary>
    public async Task SetStationIdToInterlockingObjectAsync(CancellationToken cancellationToken = default)
    {
        var interlockingObjects = await _context.InterlockingObjects
            .ToListAsync(cancellationToken);

        var updatedCount = 0;
        foreach (var interlockingObject in interlockingObjects)
        {
            var match = RegexStationId().Match(interlockingObject.Name);
            if (!match.Success)
            {
                continue;
            }

            var stationId = match.Groups[1].Value;
            interlockingObject.StationId = stationId;
            _context.Update(interlockingObject);
            updatedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Set station ID for {Count} interlocking objects", updatedCount);
    }

    /// <summary>
    ///     Initialize switching machine routes based on lock conditions
    /// </summary>
    public async Task InitializeSwitchingMachineRoutesAsync(CancellationToken cancellationToken = default)
    {
        var switchingMachinesRoutes = await _context.SwitchingMachineRoutes
            .Select(smr => new { smr.RouteId, smr.SwitchingMachineId })
            .AsAsyncEnumerable()
            .ToHashSetAsync(cancellationToken);
        var routeIds = await _context.Routes.Select(r => r.Id).ToListAsync(cancellationToken);
        var switchingMachineIds = await _context.SwitchingMachines
            .Select(sm => sm.Id)
            .AsAsyncEnumerable()
            .ToHashSetAsync(cancellationToken);
        var trackCircuitIds = await _context.TrackCircuits
            .Select(tc => tc.Id)
            .AsAsyncEnumerable()
            .ToHashSetAsync(cancellationToken);
        var directLockConditionsByRouteIds = await lockConditionRepository
            .GetConditionsByObjectIdsAndType(routeIds, LockType.Lock);
        // 進路の進路鎖錠欄
        var routeLockConditionsByRouteIds = await lockConditionRepository
            .GetConditionsByObjectIdsAndType(routeIds, LockType.Route);
        // 転てつ器のてっさ鎖錠欄
        var detectorLockConditionsBySwitchingMachineIds = await lockConditionRepository
            .GetConditionsByObjectIdsAndType(switchingMachineIds.ToList(), LockType.Detector);

        var addedCount = 0;
        foreach (var routeId in routeIds)
        {
            var directLockConditions = directLockConditionsByRouteIds.GetValueOrDefault(routeId, []);
            var routeLockConditions = routeLockConditionsByRouteIds.GetValueOrDefault(routeId, []);
            // 直接鎖錠のうち、転てつ器が条件先のものを取得する
            var targetLockConditions = directLockConditions
                .OfType<LockConditionObject>()
                .Where(lco => switchingMachineIds.Contains(lco.ObjectId))
                .ToList();
            // 進路鎖錠欄の対象ObjectId
            var targetRouteLockConditionObjectIds = routeLockConditions
                .OfType<LockConditionObject>()
                .Select(lco => lco.ObjectId)
                .ToHashSet();
            foreach (var lockCondition in targetLockConditions)
            {
                // 対象転てつ器を取得
                var switchingMachineId = lockCondition.ObjectId;
                // 既に登録済みの場合、スキップ
                if (switchingMachinesRoutes.Contains(new
                        { RouteId = routeId, SwitchingMachineId = switchingMachineId }))
                {
                    continue;
                }

                var detectorLockConditions = detectorLockConditionsBySwitchingMachineIds
                    .GetValueOrDefault(switchingMachineId, [])
                    .OfType<LockConditionObject>()
                    .Where(lco => trackCircuitIds.Contains(lco.ObjectId))
                    .ToList();
                // てっ鎖鎖錠欄に含まれている軌道回路のうち、どれか１つでも
                // 進路鎖錠欄に含まれていれば、True

                _context.SwitchingMachineRoutes.Add(new()
                {
                    RouteId = routeId,
                    SwitchingMachineId = switchingMachineId,
                    IsReverse = lockCondition.IsReverse,
                    OnRouteLock = detectorLockConditions
                        .Any(lco => targetRouteLockConditionObjectIds.Contains(lco.ObjectId))
                });
                addedCount++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} switching machine routes", addedCount);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await SetStationIdToInterlockingObjectAsync(cancellationToken);
        await InitializeSwitchingMachineRoutesAsync(cancellationToken);
    }
}