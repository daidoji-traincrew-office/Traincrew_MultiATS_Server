using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.RouteLeverDestinationButton;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;
using Traincrew_MultiATS_Server.Repositories.ThrowOutControl;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Services;

// 方針メモ: とりあえずナイーブに、全取得して処理する
// やってみて処理が重い場合は、駅ごとに処理するか、必要な対象物のみ取得するように工夫する
// Hope: クエリ自体が重すぎて時間計算量的に死ぬってことはないと信じたい

/// <summary>
///     連動装置
/// </summary>
public class RendoService(
    IRouteLeverDestinationRepository routeLeverDestinationRepository,
    IInterlockingObjectRepository interlockingObjectRepository,
    ISwitchingMachineRepository switchingMachineRepository,
    ISwitchingMachineRouteRepository switchingMachineRouteRepository,
    IDestinationButtonRepository destinationButtonRepository,
    ILockConditionRepository lockConditionRepository,
    IDateTimeRepository dateTimeRepository,
    IThrowOutControlRepository throwOutControlRepository,
    IGeneralRepository generalRepository)
{
    /// <summary>
    /// <strong>てこリレー回路</strong><br/>
    /// てこやボタンの状態から、確保するべき進路を決定する。
    /// </summary>
    /// <returns></returns>
    public async Task LeverToRouteState()
    {
        // RouteLeverDestinationButtonを全取得
        var routeLeverDestinationButtonList = await routeLeverDestinationRepository.GetAll();
        // InterlockingObjectを全取得
        var interlockingObjects = (await interlockingObjectRepository.GetAllWithState())
            .ToDictionary(obj => obj.Id);
        var routeLevers = routeLeverDestinationButtonList
            .ToDictionary(
                rdb => rdb.RouteId,
                rdb => (interlockingObjects[rdb.LeverId] as Lever)!
            );
        // Buttonを全取得
        var buttons = await destinationButtonRepository.GetAllButtons();
        // 直接鎖状条件を取得
        var lockConditions = await lockConditionRepository.GetConditionsByType(LockType.Lock);
        // 信号制御条件を取得
        var signalControlConditions = await lockConditionRepository.GetConditionsByType(LockType.SignalControl);
        // 統括制御テーブルを取得
        var throwOutControls = await throwOutControlRepository.GetAll();
        var sourceThrowOutControls = throwOutControls
            .GroupBy(c => c.SourceRouteId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var targetThrowOutControls = throwOutControls
            .GroupBy(c => c.TargetRouteId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // ここまで実行できればほぼほぼOOMしないはず
        foreach (var routeLeverDestinationButton in routeLeverDestinationButtonList)
        {
            // 対象進路 
            var route = (interlockingObjects[routeLeverDestinationButton.RouteId] as Route)!;
            var routeState = route.RouteState!;
            // この進路に対して総括制御「する」進路
            var sourceThrowOutRoutes = sourceThrowOutControls.GetValueOrDefault(route.Id, [])
                .Select(toc => interlockingObjects[toc.TargetRouteId])
                .OfType<Route>()
                .ToList();
            var hasSourceThrowOutRoute = sourceThrowOutRoutes.Count != 0;
            // この進路に対して総括制御「される」進路
            var targetThrowOutRoutes = targetThrowOutControls.GetValueOrDefault(route.Id, [])
                .Select(toc => interlockingObjects[toc.SourceRouteId])
                .OfType<Route>()
                .ToList();
            // 対象てこ
            var lever = (interlockingObjects[routeLeverDestinationButton.LeverId] as Lever)!;
            // 対象ボタン
            var button = buttons[routeLeverDestinationButton.DestinationButtonName];



            // Todo: 駅扱いてこ繋ぎ込み
            var CTCControlState = RaiseDrop.Drop;
            // Todo: CTC制御状態を確認する(CHR相当)
            //if (CTCControlState　== RaiseDrop.Raise)
            //{
            //    if (isLeverRelayRaised != /*CTCの制御条件*/)
            //    {
            //        routeState.IsLeverRelayRaised = /*CTCの制御条件*/;
            //        await generalRepository.Save(routeState);
            //    }
            //    continue;
            //}


            var isChanged = false;
            // てこ状態を取得
            var leverState = lever.LeverState.IsReversed;

            // Refactor: それぞれの条件をメソッド化した方が良い
            if (hasSourceThrowOutRoute)
            {
                var isThrowOutXRRelayRaised =
                    (!lockConditions.TryGetValue(routeLeverDestinationButton.RouteId, out var lockCondition) || IsLocked(lockCondition, interlockingObjects))
                    &&
                    sourceThrowOutRoutes
                        //各進路の根本レバーの状態を取得し、いずれかが倒れているか
                        .Select(r => routeLevers[r.Id])
                        .Any(l => l.LeverState.IsReversed != LCR.Center)
                    &&
                    (
                        (
                            sourceThrowOutRoutes.All(r => r.RouteState.IsLeverRelayRaised == RaiseDrop.Drop)
                            &&
                            leverState == LCR.Center
                        )
                        ||
                        routeState.IsLeverRelayRaised == RaiseDrop.Raise
                    )
                    &&
                    (
                        routeState.IsLeverRelayRaised == RaiseDrop.Drop ||
                        routeState.IsThrowOutXRRelayRaised == RaiseDrop.Raise
                    ) ? RaiseDrop.Raise : RaiseDrop.Drop;
                if (routeState.IsThrowOutXRRelayRaised != isThrowOutXRRelayRaised)
                {
                    isChanged = true;
                }
                routeState.IsThrowOutXRRelayRaised = isThrowOutXRRelayRaised;

                // 自進路の鎖錠欄に書かれている進路のRouteStateを取得
                var lockRouteStates = lockCondition
                    .OfType<LockConditionObject>()
                    .Select(l => interlockingObjects[l.ObjectId])
                    .OfType<Route>()
                    .Select(r => r.RouteState)
                    .OfType<RouteState>()
                    .ToList();
                // Todo: 自進路の接近鎖錠欄に書かれている条件のうち最終の条件の状態を取得 短絡していない/短絡しているが秒数が経過している
                var finalApproachTrackState = true;
                // 自進路の信号制御欄に書かれている条件に書かれている軌道回路の状態を取得
                var signalControlLockTrackCircuitState = signalControlConditions[route.Id]
                    .OfType<LockConditionObject>()
                    .Select(l => interlockingObjects[l.ObjectId])
                    .OfType<TrackCircuit>()
                    .Select(tc => tc.TrackCircuitState)
                    .ToList();
                var isThrowOutYSRelayRaised =
                    lockRouteStates.All(rs => rs.IsRouteLockRaised == RaiseDrop.Raise)
                    &&
                    (
                        sourceThrowOutRoutes.All(
                            r =>
                                r.RouteState.IsRouteLockRaised == RaiseDrop.Drop
                                &&
                                r.RouteState.IsSignalControlRaised == RaiseDrop.Drop
                        )
                        ||
                        routeState.IsThrowOutXRRelayRaised == RaiseDrop.Raise
                    )
                    &&
                    (
                        finalApproachTrackState
                        ||
                        (
                            routeState.IsThrowOutYSRelayRaised == RaiseDrop.Raise
                            &&
                            signalControlLockTrackCircuitState.All(tcs => !tcs.IsShortCircuit)
                        )
                    ) ? RaiseDrop.Raise : RaiseDrop.Drop;
                if (routeState.IsThrowOutYSRelayRaised != isThrowOutYSRelayRaised)
                {
                    isChanged = true;
                }
                routeState.IsThrowOutYSRelayRaised = isThrowOutYSRelayRaised;
            }

            var isLeverRelayRaised =
                (!lockConditions.TryGetValue(routeLeverDestinationButton.RouteId, out var lockCondition) || IsLocked(lockCondition, interlockingObjects))
                &&
                (
                    (
                        routeState is { IsThrowOutYSRelayRaised: RaiseDrop.Drop, IsThrowOutXRRelayRaised: RaiseDrop.Drop }
                        &&
                        leverState is LCR.Left or LCR.Right
                    )
                    ||
                    routeState.IsThrowOutYSRelayRaised == RaiseDrop.Raise
                )
                &&
                (
                    button.DestinationButtonState.IsRaised == RaiseDrop.Raise
                    ||
                    (
                        routeState.IsLeverRelayRaised == RaiseDrop.Raise
                        && targetThrowOutRoutes.All(r => r.RouteState.IsThrowOutXRRelayRaised == RaiseDrop.Drop)
                    )
                    ||
                    targetThrowOutRoutes.Any(r =>
                        r.RouteState.IsLeverRelayRaised == RaiseDrop.Raise
                        &&
                        r.RouteState.IsThrowOutXRRelayRaised == RaiseDrop.Raise
                    )
                ) ? RaiseDrop.Raise : RaiseDrop.Drop;
            if (routeState.IsLeverRelayRaised != isLeverRelayRaised)
            {
                isChanged = true;
            }
            routeState.IsLeverRelayRaised = isLeverRelayRaised;

            if (!isChanged)
            {
                continue;
            }
            await generalRepository.Save(routeState);
        }
    }

    /// <summary>
    /// <strong>進路照査リレー回路</strong><br/>
    /// 現在の状態から、進路が確保されているか決定する。
    /// </summary>
    /// <returns></returns>   
    public async Task RouteRelay()
    {
        // 全てのObjectを取得
        var interlockingObjects = (await interlockingObjectRepository.GetAllWithState())
            .ToDictionary(obj => obj.Id);
        // 全進路リスト
        var allRoutes = interlockingObjects
            .Select(x => x.Value)
            .OfType<Route>()
            .ToList();
        // 操作対象の進路を全て取得(てこリレーと進路照査リレーどちらかが扛上している進路)
        var routes = allRoutes
            .Where(route => route.RouteState.IsLeverRelayRaised == RaiseDrop.Raise
                            || route.RouteState.IsRouteRelayRaised == RaiseDrop.Raise)
            .ToList();
        // その中のうち、てこリレーが扛上している(かつ、進路照査リレーが落下している)進路のIDを全て取得
        var raisedRoutesIds = routes
            .Where(route => route.RouteState!.IsLeverRelayRaised == RaiseDrop.Raise)
            .Select(route => route.Id)
            .ToList();
        // てこリレーが扛上している進路の直接鎖状条件を取得
        var directLockCondition = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            raisedRoutesIds, LockType.Lock);
        // てこリレーが扛上している進路の信号制御欄を取得
        var signalControlConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            raisedRoutesIds, LockType.SignalControl);
        foreach (var route in routes)
        {
            // Todo: Refactor これ全部条件渡す必要なくね？
            var result = await ProcessRouteRelay(route, directLockCondition, signalControlConditions, interlockingObjects);
            if (route.RouteState.IsRouteRelayRaised == result)
            {
                continue;
            }
            route.RouteState.IsRouteRelayRaised = result;
            await generalRepository.Save(route.RouteState);
        }
    }

    private async Task<RaiseDrop> ProcessRouteRelay(Route route, Dictionary<ulong, List<LockCondition>> directLockCondition, Dictionary<ulong, List<LockCondition>> signalControlConditions, Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        // てこリレーが落下している場合、進路リレーを落下させてcontinue
        if (route.RouteState.IsLeverRelayRaised == RaiseDrop.Drop)
        {
            return RaiseDrop.Drop;
        }

        // 進路の鎖錠欄の条件を満たしているかを取得
        // 転轍器では、目的方向で鎖錠していること
        // 進路ではその進路の進路リレーが扛上していないこと
        Func<LockConditionObject, InterlockingObject, bool> predicate = (lockConditionObject, interlockingObject) =>
        {
            return interlockingObject switch
            {
                SwitchingMachine switchingMachine =>
                    !switchingMachine.SwitchingMachineState.IsSwitching
                    && switchingMachine.SwitchingMachineState.IsReverse == lockConditionObject.IsReverse,
                Route route => route.RouteState.IsRouteRelayRaised == RaiseDrop.Drop,
                TrackCircuit trackCircuit => !trackCircuit.TrackCircuitState.IsShortCircuit,
                _ => false
            };
        };
        if (!EvaluateLockConditions(directLockCondition[route.Id], interlockingObjects, predicate))
        {
            return RaiseDrop.Drop;
        }

        // 進路の信号制御欄の条件を満たしているか確認  
        // 軌道回路=>短絡してない
        // 転てつ器=>転換中でなく、目的方向であること
        // Todo: 進路=>保留 (仮で一旦除外)
        // Todo: てこ=>保留(仮で一旦除外)
        predicate = (o, interlockingObject) =>
        {
            return interlockingObject switch
            {
                TrackCircuit trackCircuit => !trackCircuit.TrackCircuitState.IsShortCircuit,
                SwitchingMachine switchingMachine =>
                    !switchingMachine.SwitchingMachineState.IsSwitching
                    && switchingMachine.SwitchingMachineState.IsReverse == o.IsReverse,
                Route or Lever => true,
                _ => false
            };
        };
        var signalControlState =
            EvaluateLockConditions(signalControlConditions[route.Id], interlockingObjects, predicate);
        if (!signalControlState)
        {
            return RaiseDrop.Drop;
        }

        // 鎖錠確認 進路の鎖錠欄の条件を満たしていない場合早期continue
        if (IsLocked(directLockCondition[route.Id], interlockingObjects))
        {
            return RaiseDrop.Drop;
        }
        // 進路のRouteState.IsRouteRelayRaisedをRaiseにする
        return RaiseDrop.Raise;
    }

    /// <summary>
    /// <strong>信号制御リレー回路</strong><br/>
    /// 現在の状態から、進行を指示する信号を現示してよいか決定する。
    /// </summary>
    /// <returns></returns>
    public async Task SignalControl()
    {
        // 全てのObjectを取得
        var interlockingObjects = (await interlockingObjectRepository.GetAllWithState())
            .ToDictionary(obj => obj.Id);
        // 全進路リスト
        var allRoutes = interlockingObjects
            .Select(x => x.Value)
            .OfType<Route>()
            .ToList();
        // 操作対象の進路を全て取得(進路照査リレーと信号制御リレーどちらかが扛上している進路)
        var routes = allRoutes
            .Where(route => route.RouteState.IsRouteRelayRaised == RaiseDrop.Raise
                            || route.RouteState.IsSignalControlRaised == RaiseDrop.Raise)
            .ToList();
        // その中のうち、進路照査リレーが扛上している(かつ、信号制御リレーが落下している)進路のIDを全て取得 
        var raisedRoutesIds = routes
            .Where(route => route.RouteState!.IsRouteRelayRaised == RaiseDrop.Raise)
            .Select(route => route.Id)
            .ToList();
        // 直接鎖状条件を取得
        var directLockCondition = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            raisedRoutesIds, LockType.Lock);
        // 信号制御欄を取得
        var signalControlConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            raisedRoutesIds, LockType.SignalControl);
        // Forget: 進路が定反を転換する転てつ器のてっさ鎖錠が落下している(進路照査リレーでみているため)

        foreach (var route in routes)
        {
            // Todo: Refactor これ全部条件渡す必要なくね？
            var result = await ProcessSignalControl(route, directLockCondition, signalControlConditions, interlockingObjects);
            if (route.RouteState.IsSignalControlRaised == result)
            {
                continue;
            }
            route.RouteState.IsSignalControlRaised = result;
            await generalRepository.Save(route.RouteState);
        }
    }

    private async Task<RaiseDrop> ProcessSignalControl(Route route, Dictionary<ulong, List<LockCondition>> directLockCondition, Dictionary<ulong, List<LockCondition>> signalControlConditions, Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        // 進路照査リレーが落下している場合、信号制御リレーを落下させてcontinue
        if (route.RouteState.IsRouteRelayRaised == RaiseDrop.Drop)
        {
            return RaiseDrop.Drop;
        }
        // 信号制御欄の条件を満たしているか
        var predicate = new Func<LockConditionObject, InterlockingObject, bool>((o, interlockingObject) =>
        {
            return interlockingObject switch
            {
                // 軌道回路が短絡していないこと
                TrackCircuit trackCircuit => !trackCircuit.TrackCircuitState.IsShortCircuit,
                // 進路のてこリレーが落下していること
                Route targetRoute => targetRoute.RouteState.IsLeverRelayRaised == RaiseDrop.Drop,
                SwitchingMachine or Lever => true,
                _ => false
            };
        });
        if (!EvaluateLockConditions(signalControlConditions[route.Id], interlockingObjects, predicate))
        {
            return RaiseDrop.Drop;
        }

        // 鎖錠確認 進路の鎖錠欄の条件を満たしていない場合早期continue
        if (IsLocked(directLockCondition[route.Id], interlockingObjects))
        {
            return RaiseDrop.Drop;
        }
        // Question: 鎖状確認 信号制御欄の条件を満たしているか確認?

        // 進路のRouteState.IsSignalControlRaisedを扛上させる
        return RaiseDrop.Raise;
    }

    private bool IsLocked(List<LockCondition> lockConditions, Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        // 対象が進路のものに限る
        // 対象進路のisLeverRelayRaisedがすべてDropであることを確認する
        return EvaluateLockConditions(lockConditions, interlockingObjects, Predicate);

        bool Predicate(LockConditionObject o, InterlockingObject interlockingObject)
        {
            return interlockingObject switch
            {
                Route route => route.RouteState.IsLeverRelayRaised == RaiseDrop.Raise,
                TrackCircuit trackCircuit => trackCircuit.TrackCircuitState.IsShortCircuit,
                SwitchingMachine or Route or Lever => false,
                _ => true
            };
        }
    }

    internal static bool IsDetectorLocked(List<LockCondition> lockConditions,
        Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        return EvaluateLockConditions(lockConditions, interlockingObjects, Predicate);

        // 軌道回路は短絡していないか、同時に鎖錠されていないか
        bool Predicate(LockConditionObject o, InterlockingObject interlockingObject)
        {
            return interlockingObject switch
            {
                TrackCircuit trackCircuit =>
                    trackCircuit.TrackCircuitState is not { IsShortCircuit: false, IsLocked: false },
                SwitchingMachine or Lever or Route => false,
                _ => true
            };
        }
    }

    /// <summary>
    /// <strong>鎖錠確認</strong><br/>
    /// 鎖状の条件を述語をもとに確認する
    /// </summary>
    /// <returns></returns>
    private static bool EvaluateLockConditions(
        List<LockCondition> lockConditions,
        Dictionary<ulong, InterlockingObject> interlockingObjects,
        Func<LockConditionObject, InterlockingObject, bool> predicate)
    {
        var rootLockConditions = lockConditions.Where(lc => lc.ParentId == null).ToList();
        var childLockConditions = lockConditions
            .Where(lc => lc.ParentId != null)
            .GroupBy(lc => lc.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        return rootLockConditions.All(lockCondition =>
            EvaluateLockCondition(lockCondition, childLockConditions, interlockingObjects, predicate));
    }

    /// <summary>
    /// <strong>鎖錠確認</strong><br/>
    /// 鎖状の条件を述語をもとに1つ確認する
    /// </summary>
    /// <returns></returns>
    private static bool EvaluateLockCondition(LockCondition lockCondition,
        Dictionary<ulong, List<LockCondition>> childLockConditions,
        Dictionary<ulong, InterlockingObject> interlockingObjects,
        Func<LockConditionObject, InterlockingObject, bool> predicate)
    {
        switch (lockCondition.Type)
        {
            case LockConditionType.And:
                return childLockConditions[lockCondition.Id].All(childLockCondition =>
                    EvaluateLockCondition(childLockCondition, childLockConditions, interlockingObjects, predicate));
            case LockConditionType.Or:
                return childLockConditions[lockCondition.Id].Any(childLockCondition =>
                    EvaluateLockCondition(childLockCondition, childLockConditions, interlockingObjects, predicate));
        }

        if (lockCondition is not LockConditionObject lockConditionObject)
        {
            // And, Or以外だとこれしかないので、基本的にはこのルートには入らない想定
            return false;
        }

        return interlockingObjects.TryGetValue(lockConditionObject.ObjectId, out var interlockingObject)
               && predicate(lockConditionObject, interlockingObject);
    }
}