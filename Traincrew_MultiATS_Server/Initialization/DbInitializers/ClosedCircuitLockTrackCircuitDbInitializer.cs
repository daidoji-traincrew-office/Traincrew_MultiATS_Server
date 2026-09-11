using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.ClosedCircuitLockTrackCircuit;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     鎖錠条件から閉路鎖錠対象の軌道回路(進路鎖錠欄にあり、信号制御欄になく、鎖錠欄にある軌道回路)を導出して初期化する
/// </summary>
public class ClosedCircuitLockTrackCircuitDbInitializer(
    ILogger<ClosedCircuitLockTrackCircuitDbInitializer> logger,
    IRouteRepository routeRepository,
    ITrackCircuitRepository trackCircuitRepository,
    ILockConditionRepository lockConditionRepository,
    IClosedCircuitLockTrackCircuitRepository closedCircuitLockTrackCircuitRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer
{
    /// <summary>
    ///     鎖錠条件(進路鎖錠欄・鎖錠欄・信号制御欄)から閉路鎖錠対象の軌道回路を導出し、DBへ反映する
    /// </summary>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var routeIds = await routeRepository.GetIdsForAll();
        var trackCircuitIds = (await trackCircuitRepository.GetAllTrackCircuitList(cancellationToken))
            .Select(trackCircuit => trackCircuit.Id)
            .ToHashSet();

        var routeLockConditionsByObjectId = await lockConditionRepository.GetConditionsByType(LockType.Route);
        var directLockConditionsByObjectId = await lockConditionRepository.GetConditionsByType(LockType.Lock);
        var signalControlConditionsByObjectId = await lockConditionRepository.GetConditionsByType(LockType.SignalControl);

        var closedCircuitLockTrackCircuitList = routeIds
            .SelectMany(routeId =>
            {
                var routeTcIds = ExtractTrackCircuitIds(routeLockConditionsByObjectId, routeId, trackCircuitIds);
                var lockTcIds = ExtractTrackCircuitIds(directLockConditionsByObjectId, routeId, trackCircuitIds);
                var signalControlTcIds = ExtractTrackCircuitIds(signalControlConditionsByObjectId, routeId, trackCircuitIds);

                return CalculateClosedCircuitLockTrackCircuitIds(routeTcIds, lockTcIds, signalControlTcIds)
                    .Select(trackCircuitId => new ClosedCircuitLockTrackCircuit
                    {
                        RouteId = routeId,
                        TrackCircuitId = trackCircuitId
                    });
            })
            .ToList();

        var existingList = await closedCircuitLockTrackCircuitRepository.GetAll(cancellationToken);
        var newPairs = closedCircuitLockTrackCircuitList
            .Select(x => (x.RouteId, x.TrackCircuitId))
            .ToHashSet();
        var existingPairs = existingList
            .Select(x => (x.RouteId, x.TrackCircuitId))
            .ToHashSet();

        var toAdd = closedCircuitLockTrackCircuitList
            .Where(x => !existingPairs.Contains((x.RouteId, x.TrackCircuitId)))
            .ToList();
        var toDelete = existingList
            .Where(x => !newPairs.Contains((x.RouteId, x.TrackCircuitId)))
            .ToList();

        await generalRepository.AddAll(toAdd, cancellationToken);
        await generalRepository.DeleteAll(toDelete, cancellationToken);

        logger.LogInformation(
            "Initialized closed circuit lock track circuits: {AddedCount} added, {DeletedCount} deleted",
            toAdd.Count, toDelete.Count);
    }

    /// <summary>
    ///     指定した進路(ObjectId)の鎖錠条件から、軌道回路のObjectIdのみを抽出する
    /// </summary>
    private static IEnumerable<ulong> ExtractTrackCircuitIds(
        Dictionary<ulong, List<LockCondition>> lockConditionsByObjectId,
        ulong objectId,
        IReadOnlySet<ulong> trackCircuitIds)
    {
        if (!lockConditionsByObjectId.TryGetValue(objectId, out var lockConditions))
        {
            return [];
        }

        return lockConditions
            .OfType<LockConditionObject>()
            .Select(lockConditionObject => lockConditionObject.ObjectId)
            .Where(trackCircuitIds.Contains);
    }

    /// <summary>
    ///     閉路鎖錠対象の軌道回路ID(進路鎖錠欄にあり、信号制御欄になく、鎖錠欄にある軌道回路)を算出する
    /// </summary>
    /// <param name="routeTcIds">進路鎖錠欄にある軌道回路ID</param>
    /// <param name="lockTcIds">鎖錠欄にある軌道回路ID</param>
    /// <param name="signalControlTcIds">信号制御欄にある軌道回路ID</param>
    /// <returns>閉路鎖錠対象の軌道回路ID</returns>
    public static HashSet<ulong> CalculateClosedCircuitLockTrackCircuitIds(
        IEnumerable<ulong> routeTcIds,
        IEnumerable<ulong> lockTcIds,
        IEnumerable<ulong> signalControlTcIds)
    {
        var lockTcIdSet = lockTcIds.ToHashSet();
        var signalControlTcIdSet = signalControlTcIds.ToHashSet();

        return routeTcIds
            .Where(trackCircuitId =>
                lockTcIdSet.Contains(trackCircuitId) && !signalControlTcIdSet.Contains(trackCircuitId))
            .ToHashSet();
    }
}
