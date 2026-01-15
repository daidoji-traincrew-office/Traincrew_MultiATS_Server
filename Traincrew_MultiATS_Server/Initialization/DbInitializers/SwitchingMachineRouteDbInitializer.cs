using System.Text.RegularExpressions;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes switching machine route relationships and sets station IDs for interlocking objects
/// </summary>
public partial class SwitchingMachineRouteDbInitializer(
    ILogger<SwitchingMachineRouteDbInitializer> logger,
    IInterlockingObjectRepository interlockingObjectRepository,
    ISwitchingMachineRepository switchingMachineRepository,
    ISwitchingMachineRouteRepository switchingMachineRouteRepository,
    IRouteRepository routeRepository,
    ITrackCircuitRepository trackCircuitRepository,
    ILockConditionRepository lockConditionRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer(logger)
{
    [GeneratedRegex(@"^(TH(\d{1,2}S?))_")]
    private static partial Regex RegexStationId();

    /// <summary>
    ///     Set station ID to interlocking objects based on naming pattern
    /// </summary>
    public async Task SetStationIdToInterlockingObjectAsync(CancellationToken cancellationToken = default)
    {
        var interlockingObjects = await interlockingObjectRepository.GetAllAsync(cancellationToken);

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
            interlockingObjectRepository.Update(interlockingObject);
            updatedCount++;
        }

        await interlockingObjectRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Set station ID for {Count} interlocking objects", updatedCount);
    }

    /// <summary>
    ///     Initialize switching machine routes based on lock conditions
    /// </summary>
    public async Task InitializeSwitchingMachineRoutesAsync(CancellationToken cancellationToken = default)
    {
        var switchingMachinesRoutes = await switchingMachineRouteRepository.GetAllPairsAsync(cancellationToken);
        var routeIds = await routeRepository.GetIdsForAll();
        var switchingMachineIds = await switchingMachineRepository.GetAllIdsAsync(cancellationToken);
        var trackCircuitIds = await trackCircuitRepository.GetAllNames(cancellationToken);
        var trackCircuitIdByName = await trackCircuitRepository.GetIdsByName(cancellationToken);
        var trackCircuitIdsSet = trackCircuitIdByName.Values.ToHashSet();

        var directLockConditionsByRouteIds = await lockConditionRepository
            .GetConditionsByObjectIdsAndType(routeIds, LockType.Lock);
        // 進路の進路鎖錠欄
        var routeLockConditionsByRouteIds = await lockConditionRepository
            .GetConditionsByObjectIdsAndType(routeIds, LockType.Route);
        // 転てつ器のてっさ鎖錠欄
        var detectorLockConditionsBySwitchingMachineIds = await lockConditionRepository
            .GetConditionsByObjectIdsAndType(switchingMachineIds.ToList(), LockType.Detector);

        var switchingMachineRoutesToAdd = new List<SwitchingMachineRoute>();
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
                if (switchingMachinesRoutes.Contains((routeId, switchingMachineId)))
                {
                    continue;
                }

                var detectorLockConditions = detectorLockConditionsBySwitchingMachineIds
                    .GetValueOrDefault(switchingMachineId, [])
                    .OfType<LockConditionObject>()
                    .Where(lco => trackCircuitIdsSet.Contains(lco.ObjectId))
                    .ToList();
                // てっ鎖鎖錠欄に含まれている軌道回路のうち、どれか１つでも
                // 進路鎖錠欄に含まれていれば、True

                switchingMachineRoutesToAdd.Add(new()
                {
                    RouteId = routeId,
                    SwitchingMachineId = switchingMachineId,
                    IsReverse = lockCondition.IsReverse,
                    OnRouteLock = detectorLockConditions
                        .Any(lco => targetRouteLockConditionObjectIds.Contains(lco.ObjectId))
                });
            }
        }

        await generalRepository.AddAll(switchingMachineRoutesToAdd);
        _logger.LogInformation("Initialized {Count} switching machine routes", switchingMachineRoutesToAdd.Count);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await SetStationIdToInterlockingObjectAsync(cancellationToken);
        await InitializeSwitchingMachineRoutesAsync(cancellationToken);
    }
}