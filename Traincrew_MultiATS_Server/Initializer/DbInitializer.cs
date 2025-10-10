using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Initializer;

internal partial class DbInitializer(
    DBBasejson DBBase,
    ApplicationDbContext context,
    ILockConditionRepository lockConditionRepository,
    ILogger<DbInitializer> logger,
    CancellationToken cancellationToken)
{
    [GeneratedRegex(@"^(TH(\d{1,2}S?))_")]
    private static partial Regex RegexStationId();

    internal async Task Initialize()
    {
        await InitStation();
        await InitStationTimerState();
        await InitTrackCircuit();
        await InitSignalType();
    }

    internal async Task InitializeAfterCreateRoute()
    {
        await InitSignal();
        await InitNextSignal();
        await InitTrackCircuitSignal();
        await InitializeSignalRoute();
        await InitializeThrowOutControl();
    }

    internal async Task InitializeAfterCreateLockCondition()
    {
        await InitializeSwitchingMachineRoutes();
        await SetStationIdToInterlockingObject();
    }

    private async Task InitStation()
    {
        var stationNames = (await context.Stations
            .Select(s => s.Name)
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var station in DBBase.stationList)
        {
            if (stationNames.Contains(station.Name))
            {
                continue;
            }

            context.Stations.Add(new()
            {
                Id = station.Id,
                Name = station.Name,
                IsStation = station.IsStation,
                IsPassengerStation = station.IsPassengerStation
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitStationTimerState()
    {
        var stationIds = (await context.Stations
            .Where(s => s.IsStation)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken)).ToHashSet();
        var stationTimerStates = (await context.StationTimerStates
            .Select(s => new { s.StationId, s.Seconds })
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var stationId in stationIds)
        {
            foreach (var seconds in new[] { 30, 60 })
            {
                if (stationTimerStates.Contains(new { StationId = stationId, Seconds = seconds }))
                {
                    continue;
                }

                context.StationTimerStates.Add(new()
                {
                    StationId = stationId,
                    Seconds = seconds,
                    IsTeuRelayRaised = RaiseDrop.Drop,
                    IsTenRelayRaised = RaiseDrop.Drop,
                    IsTerRelayRaised = RaiseDrop.Raise,
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitTrackCircuit()
    {
        // 全軌道回路情報を取得
        var trackCircuitNames = (await context.TrackCircuits
            .Select(tc => tc.Name)
            .ToListAsync(cancellationToken)).ToHashSet();

        foreach (var item in DBBase.trackCircuitList)
        {
            if (trackCircuitNames.Contains(item.Name))
            {
                continue;
            }

            context.TrackCircuits.Add(new()
            {
                // Todo: ProtectionZoneの未定義部分がなくなったら、ProtectionZoneのデフォルト値の設定を解除
                ProtectionZone = item.ProtectionZone ?? 99,
                Name = item.Name,
                Type = ObjectType.TrackCircuit,
                TrackCircuitState = new()
                {
                    IsShortCircuit = false,
                    IsLocked = false,
                    TrainNumber = "",
                    IsCorrectionDropRelayRaised = RaiseDrop.Drop,
                    IsCorrectionRaiseRelayRaised = RaiseDrop.Drop,
                    DroppedAt = null,
                    RaisedAt = null,
                }
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitSignal()
    {
        // 軌道回路情報を取得
        var trackCircuits = await context.TrackCircuits
            .Select(tc => new { tc.Id, tc.Name })
            .ToDictionaryAsync(tc => tc.Name, tc => tc.Id, cancellationToken);
        // 既に登録済みの信号情報を取得
        var signalNames = (await context.Signals
            .Select(s => s.Name)
            .ToListAsync(cancellationToken)).ToHashSet();
        // 駅マスタから停車場を取得
        var stations = await context.Stations
            .Where(station => station.IsStation)
            .ToListAsync(cancellationToken);
        // DirectionRoutesを事前に取得してDictionaryに格納
        var directionRoutes = await context.DirectionRoutes
            .ToDictionaryAsync(dr => dr.Name, dr => dr.Id, cancellationToken);

        // 信号情報登録
        foreach (var signalData in DBBase.signalDataList)
        {
            // 既に登録済みの場合、スキップ
            if (signalNames.Contains(signalData.Name))
            {
                continue;
            }

            // 軌道回路初期化
            ulong trackCircuitId = 0;
            // 明示的に指定された軌道回路名がある場合はそれを使用
            if (signalData.TrackCircuitName != null)
            {
                trackCircuits.TryGetValue(signalData.TrackCircuitName, out trackCircuitId);
            }
            // それ以外で閉塞信号機の場合、閉塞信号機の軌道回路を使う
            else if (signalData.Name.StartsWith("上り閉塞") || signalData.Name.StartsWith("下り閉塞"))
            {
                var trackCircuitName = $"{signalData.Name.Replace("閉塞", "")}T";
                trackCircuits.TryGetValue(trackCircuitName, out trackCircuitId);
            }

            var stationId = stations
                .Where(s => signalData.Name.StartsWith(s.Name))
                .Select(s => s.Id)
                .FirstOrDefault();

            // 方向進路および方向の初期化
            ulong? directionRouteLeftId = null;
            ulong? directionRouteRightId = null;
            if (signalData.DirectionRouteLeft != null)
            {
                if (!directionRoutes.TryGetValue(signalData.DirectionRouteLeft, out var directionRouteId))
                {
                    throw new InvalidOperationException($"方向進路が見つかりません: {signalData.DirectionRouteLeft}");
                }

                directionRouteLeftId = directionRouteId;
            }

            if (signalData.DirectionRouteRight != null)
            {
                if (!directionRoutes.TryGetValue(signalData.DirectionRouteRight, out var directionRouteId))
                {
                    throw new InvalidOperationException($"方向進路が見つかりません: {signalData.DirectionRouteRight}");
                }

                directionRouteRightId = directionRouteId;
            }

            LR? direction = signalData.Direction != null
                ? signalData.Direction == "L" ? LR.Left : signalData.Direction == "R" ? LR.Right : null
                : null;

            context.Signals.Add(new()
            {
                Name = signalData.Name,
                StationId = stationId,
                TrackCircuitId = trackCircuitId > 0 ? trackCircuitId : null,
                TypeName = signalData.TypeName,
                SignalState = new()
                {
                    IsLighted = true,
                },
                DirectionRouteLeftId = directionRouteLeftId,
                DirectionRouteRightId = directionRouteRightId,
                Direction = direction
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitSignalType()
    {
        var signalTypeNames = (await context.SignalTypes
            .Select(st => st.Name)
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var signalTypeData in DBBase.signalTypeList)
        {
            if (signalTypeNames.Contains(signalTypeData.Name))
            {
                continue;
            }

            context.SignalTypes.Add(new()
            {
                Name = signalTypeData.Name,
                RIndication = GetSignalIndication(signalTypeData.RIndication),
                YYIndication = GetSignalIndication(signalTypeData.YYIndication),
                YIndication = GetSignalIndication(signalTypeData.YIndication),
                YGIndication = GetSignalIndication(signalTypeData.YGIndication),
                GIndication = GetSignalIndication(signalTypeData.GIndication)
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static SignalIndication GetSignalIndication(string indication)
    {
        return indication switch
        {
            "R" => SignalIndication.R,
            "YY" => SignalIndication.YY,
            "Y" => SignalIndication.Y,
            "YG" => SignalIndication.YG,
            "G" => SignalIndication.G,
            _ => SignalIndication.R
        };
    }

    private async Task InitNextSignal()
    {
        const int maxDepth = 4;
        foreach (var signalData in DBBase.signalDataList)
        {
            var nextSignalNames = signalData.NextSignalNames ?? [];
            foreach (var nextSignalName in nextSignalNames)
            {
                // Todo: ここでN+1問題が発生しているので、改善したほうが良いかも
                // 既に登録済みの場合、スキップ
                if (context.NextSignals.Any(ns =>
                        ns.SignalName == signalData.Name && ns.TargetSignalName == nextSignalName))
                {
                    continue;
                }

                context.NextSignals.Add(new()
                {
                    SignalName = signalData.Name,
                    SourceSignalName = signalData.Name,
                    TargetSignalName = nextSignalName,
                    Depth = 1
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var allSignals = await context.Signals.ToListAsync(cancellationToken);
        var nextSignalList = await context.NextSignals
            .Where(ns => ns.Depth == 1)
            .GroupBy(ns => ns.SignalName)
            .ToListAsync(cancellationToken);
        var nextSignalDict = nextSignalList
            .ToDictionary(
                g => g.Key,
                g => g.Select(ns => ns.TargetSignalName).ToList()
            );
        // Todo: このロジック、絶対テスト書いたほうがいい(若干複雑な処理をしてしまったので)
        for (var depth = 2; depth <= maxDepth; depth++)
        {
            var nextNextSignalDict = await context.NextSignals
                .Where(ns => ns.Depth == depth - 1)
                .GroupBy(ns => ns.SignalName)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(ns => ns.TargetSignalName).ToList(),
                    cancellationToken
                );
            List<NextSignal> nextNextSignals = [];
            // 全信号機でループ
            foreach (var signal in allSignals)
            {
                // 次信号機がない場合はスキップ
                if (!nextNextSignalDict.TryGetValue(signal.Name, out var nextSignals))
                {
                    continue;
                }

                foreach (var nextSignal in nextSignals)
                {
                    // 次信号機の次信号機を取ってくる
                    if (!nextSignalDict.TryGetValue(nextSignal, out var nnSignals))
                    {
                        continue;
                    }

                    foreach (var nextNextSignal in nnSignals)
                    {
                        // Todo: N+1問題が発生しているので、改善したほうが良いかも
                        if (context.NextSignals.Any(ns =>
                                ns.SignalName == signal.Name && ns.TargetSignalName == nextNextSignal))
                        {
                            continue;
                        }

                        context.NextSignals.Add(new()
                        {
                            SignalName = signal.Name,
                            SourceSignalName = nextSignal,
                            TargetSignalName = nextNextSignal,
                            Depth = depth
                        });
                        await context.SaveChangesAsync(cancellationToken);
                    }
                }
            }
        }
    }

    private async Task InitTrackCircuitSignal()
    {
        foreach (var trackCircuit in DBBase.trackCircuitList)
        {
            var trackCircuitEntity = await context.TrackCircuits
                .FirstOrDefaultAsync(tc => tc.Name == trackCircuit.Name, cancellationToken);

            if (trackCircuitEntity == null) continue;
            foreach (var signalName in trackCircuit.NextSignalNamesUp ?? [])
            {
                // Todo: ここでN+1問題が発生しているので、改善したほうが良いかも
                if (context.TrackCircuitSignals.Any(tcs =>
                        tcs.TrackCircuitId == trackCircuitEntity.Id && tcs.SignalName == signalName && tcs.IsUp))
                {
                    continue;
                }

                context.TrackCircuitSignals.Add(new()
                {
                    TrackCircuitId = trackCircuitEntity.Id,
                    SignalName = signalName,
                    IsUp = true
                });
            }

            foreach (var signalName in trackCircuit.NextSignalNamesDown ?? [])
            {
                // Todo: ここでN+1問題が発生しているので、改善したほうが良いかも
                if (context.TrackCircuitSignals.Any(tcs =>
                        tcs.TrackCircuitId == trackCircuitEntity.Id && tcs.SignalName == signalName && !tcs.IsUp))
                {
                    continue;
                }

                context.TrackCircuitSignals.Add(new()
                {
                    TrackCircuitId = trackCircuitEntity.Id,
                    SignalName = signalName,
                    IsUp = false
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitializeSignalRoute()
    {
        var signalRoutes = await context.SignalRoutes
            .Include(sr => sr.Route)
            .ToListAsync(cancellationToken);
        var routes = await context.Routes
            .ToDictionaryAsync(r => r.Name, cancellationToken);
        foreach (var signal in DBBase.signalDataList)
        {
            foreach (var routeName in signal.RouteNames)
            {
                // Todo: FW 全探索なので改善したほうがいいかも
                if (signalRoutes.Any(sr => sr.SignalName == signal.Name && sr.Route.Name == routeName))
                {
                    continue;
                }

                if (!routes.TryGetValue(routeName, out var route))
                {
                    // Todo: 例外を出す
                    continue;
                }

                context.SignalRoutes.Add(new()
                {
                    SignalName = signal.Name,
                    RouteId = route.Id
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitializeThrowOutControl()
    {
        var routesByName = await context.Routes
            .ToDictionaryAsync(r => r.Name, cancellationToken);
        var directionRouteByName = await context.DirectionRoutes
            .ToDictionaryAsync(r => r.Name, cancellationToken);
        var directionSelfControlLeverByName = await context.DirectionSelfControlLevers
            .ToDictionaryAsync(r => r.Name, cancellationToken);
        var throwOutControlList = (await context.ThrowOutControls
                .Include(toc => toc.Source)
                .Include(toc => toc.Target)
                .Select(toc => new { SourceRouteName = toc.Source.Name, TargetRouteName = toc.Target.Name })
                .ToListAsync(cancellationToken))
            .ToHashSet();
        foreach (var throwOutControl in DBBase.throwOutControlList)
        {
            // 既に登録済みの場合、スキップ
            if (throwOutControlList.Contains(
                    new { throwOutControl.SourceRouteName, throwOutControl.TargetRouteName }))
            {
                continue;
            }

            InterlockingObject target;
            LR? targetLr = null;
            ulong? directionSelfControlLeverId = null;
            // 進路名を取得
            if (!routesByName.TryGetValue(throwOutControl.SourceRouteName, out var sourceRoute))
            {
                throw new InvalidOperationException($"進路名が見つかりません: {throwOutControl.SourceRouteName}");
            }

            // 方向てこ以外
            if (routesByName.TryGetValue(throwOutControl.TargetRouteName, out var targetRoute))
            {
                target = targetRoute;
            }
            // 方向てこ
            else if ((throwOutControl.TargetRouteName.EndsWith('L') || throwOutControl.TargetRouteName.EndsWith('R'))
                     && directionRouteByName.TryGetValue(throwOutControl.TargetRouteName[..^1] + 'F',
                         out var directionRoute))
            {
                target = directionRoute;
                targetLr = throwOutControl.TargetRouteName.EndsWith('L') ? LR.Left : LR.Right;
                // 該当する開放てこを探し、方向てこにも開放てこのリンクを設定する
                if (!directionSelfControlLeverByName.TryGetValue(throwOutControl.LeverConditionName[..^1],
                        out var directionSelfControlLever))
                {
                    throw new InvalidOperationException($"開放てこが見つかりません: {throwOutControl.LeverConditionName[..^1]}");
                }

                directionSelfControlLeverId = directionSelfControlLever.Id;
                directionRoute.DirectionSelfControlLeverId = directionSelfControlLeverId;
                context.DirectionRoutes.Update(directionRoute);
            }
            else
            {
                throw new InvalidOperationException($"進路名が見つかりません: {throwOutControl.TargetRouteName}");
            }

            context.ThrowOutControls.Add(new()
            {
                SourceId = sourceRoute.Id,
                TargetId = target.Id,
                TargetLr = targetLr,
                ConditionLeverId = directionSelfControlLeverId
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        var changedEntriesCopy = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Unchanged)
            .ToList();
        foreach (var entry in changedEntriesCopy)
        {
            entry.State = EntityState.Detached;
        }
    }

    private async Task SetStationIdToInterlockingObject()
    {
        var interlockingObjects = await context.InterlockingObjects
            .ToListAsync();
        foreach (var interlockingObject in interlockingObjects)
        {
            var match = RegexStationId().Match(interlockingObject.Name);
            if (!match.Success)
            {
                continue;
            }

            var stationId = match.Groups[1].Value;
            interlockingObject.StationId = stationId;
            context.Update(interlockingObject);
        }

        await context.SaveChangesAsync();
    }

    private async Task InitializeSwitchingMachineRoutes()
    {
        var switchingMachinesRoutes = await context.SwitchingMachineRoutes
            .Select(smr => new { smr.RouteId, smr.SwitchingMachineId })
            .AsAsyncEnumerable()
            .ToHashSetAsync();
        var routeIds = await context.Routes.Select(r => r.Id).ToListAsync();
        var switchingMachineIds = await context.SwitchingMachines
            .Select(sm => sm.Id)
            .AsAsyncEnumerable()
            .ToHashSetAsync();
        var trackCircuitIds = await context.TrackCircuits
            .Select(tc => tc.Id)
            .AsAsyncEnumerable()
            .ToHashSetAsync();
        var directLockConditionsByRouteIds = await lockConditionRepository
            .GetConditionsByObjectIdsAndType(routeIds, LockType.Lock);
        // 進路の進路鎖錠欄
        var routeLockConditionsByRouteIds = await lockConditionRepository
            .GetConditionsByObjectIdsAndType(routeIds, LockType.Route);
        // 転てつ器のてっさ鎖錠欄
        var detectorLockConditionsBySwitchingMachineIds = await lockConditionRepository
            .GetConditionsByObjectIdsAndType(switchingMachineIds.ToList(), LockType.Detector);
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

                context.SwitchingMachineRoutes.Add(new()
                {
                    RouteId = routeId,
                    SwitchingMachineId = switchingMachineId,
                    IsReverse = lockCondition.IsReverse,
                    OnRouteLock = detectorLockConditions
                        .Any(lco => targetRouteLockConditionObjectIds.Contains(lco.ObjectId)),
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
