using System.Linq;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.RouteLeverDestinationButton;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;
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
        // Buttonを全取得
        var buttons = await destinationButtonRepository.GetAllButtons();
        // 直接鎖状条件を取得
        var lockConditions = await lockConditionRepository.GetConditionsByType(LockType.Lock);

        // ここまで実行できればほぼほぼOOMしないはず
        foreach (var routeLeverDestinationButton in routeLeverDestinationButtonList)
        {
            // 対象進路 
            var route = (interlockingObjects[routeLeverDestinationButton.RouteId] as Route)!;
            var routeState = route.RouteState!;
            // この進路に対して総括制御「する」進路
            // Todo: この進路に対して総括制御「する」進路を取得する
            var sourceThrowOutRoute = new List<Route>();
            var hasSourceThrowOutRoute = sourceThrowOutRoute.Count != 0;
            // この進路に対して総括制御「される」進路
            // Todo: この進路に対して総括制御「される」進路を取得する
            var targetThrowOutRoute = new List<Route>();
            var hasTargetThrowOutRoute = sourceThrowOutRoute.Count != 0;
            // 対象てこ
            var lever = (interlockingObjects[routeLeverDestinationButton.LeverId] as Lever)!;
            // 対象ボタン
            var button = buttons[routeLeverDestinationButton.DestinationButtonName];

            // 鎖錠確認 進路の鎖錠欄の条件を満たしていない場合早期continue
            if (IsLocked(lockConditions[routeLeverDestinationButton.RouteId], interlockingObjects))
            {
                continue;
            }

            // Todo: CTC制御状態を確認する(CHR相当)

            var isLeverRelayRaised = routeState.IsLeverRelayRaised;

            // てこ状態を取得
            var leverState = lever.LeverState.IsReversed;

            // 総括関係

            // 総括される関係
            if (hasSourceThrowOutRoute)
            {
                foreach (var sourceRoute in sourceThrowOutRoute)
                {
                    var isThrowOutRaised = routeState.IsThrowOutXRRelayRaised;
                    // てこありホーム軌道回路分割
                    // Todo: 総括してくる進路に対応するレバーが倒れている場合
                    if (LCR.Left is LCR.Left or LCR.Right)
                    {
                        // Todo: [繋ぎ込み]sourceRouteに対応する着点ボタンを押していない場合
                        var sourceButton = buttons[""];

                        var para1 = false;
                        if ((sourceButton.DestinationButtonState.IsRaised != RaiseDrop.Raise && leverState == LCR.Center || route.RouteState.IsLeverRelayRaised == RaiseDrop.Raise) && (routeState.IsLeverRelayRaised == RaiseDrop.Drop || routeState.IsThrowOutXRRelayRaised == RaiseDrop.Raise))
                        {
                            routeState.IsThrowOutXRRelayRaised = RaiseDrop.Raise;
                        }
                        else
                        {
                            routeState.IsThrowOutXRRelayRaised = RaiseDrop.Drop;
                        }
                        // YSBP相当が必要かどうかはわからない


                    }
                    else
                    {
                        routeState.IsThrowOutXRRelayRaised = RaiseDrop.Drop;
                    }
                    // --てこありホーム軌道回路分割

                    await generalRepository.Save(routeState);
                }
            }

            // 単独・総括する関係
            // てこが中立位置の場合
            if (leverState == LCR.Center)
            {
                // リレーが上がっている場合は下げる
                if (isLeverRelayRaised == RaiseDrop.Raise)
                {
                    routeState.IsLeverRelayRaised = RaiseDrop.Drop;
                    await generalRepository.Save(routeState);
                }

                // てこありホーム軌道回路分割
                // [総括:YS相当の支障チェック]
                var finalApproachTrackState = new TrackCircuitState();
                var initialSignalLockTrackState = new TrackCircuitState();
                if ((finalApproachTrackState.IsLocked && sourceThrowOutRoute.Any(route => route.RouteState.IsSignalControlRaised == RaiseDrop.Raise)) || routeState.IsThrowOutXRRelayRaised == RaiseDrop.Raise && (finalApproachTrackState.IsLocked || (routeState.IsThrowOutYSRelayRaised == RaiseDrop.Raise && !initialSignalLockTrackState.IsShortCircuit)))
                {
                    routeState.IsThrowOutYSRelayRaised = RaiseDrop.Raise;
                    await generalRepository.Save(routeState);
                }
                else
                {
                    routeState.IsThrowOutYSRelayRaised = RaiseDrop.Drop;
                    await generalRepository.Save(routeState);
                }
                // --てこありホーム軌道回路分割

                if (routeState.IsThrowOutYSRelayRaised == RaiseDrop.Raise && )
                {
                    routeState.IsLeverRelayRaised = RaiseDrop.Raise;
                    await generalRepository.Save(routeState);
                }

                continue;
            }

            // [総括:総括されるフラグと]てこが倒れている場合
            if (leverState is LCR.Left or LCR.Right && routeState.IsThrowOutXRRelayRaised == RaiseDrop.Drop)
            {
                // すでにてこリレーが上がっている場合はスキップ
                if (isLeverRelayRaised == RaiseDrop.Raise)
                {
                    continue;
                }
                var isRaised = button.DestinationButtonState.IsRaised;
                // [着点ボタンが押されている場合は、てこリレーを上げる
                if (isRaised == RaiseDrop.Raise)
                {
                    routeState.IsLeverRelayRaised = RaiseDrop.Raise;
                    await generalRepository.Save(routeState);
                }
                // 総括しにいく進路の総括フラグとてこリレーが上がっている場合は、てこリレーを上げる
                else if (targetThrowOutRoute.Any(EvaluateThrowOutCondition))
                {
                    routeState.IsLeverRelayRaised = RaiseDrop.Raise;
                    await generalRepository.Save(routeState);
                }
            }
        }
    }

    /// <summary>
    /// <strong>転てつ器制御回路・転てつ器表示リレー回路</strong><br/>
    /// 現在の状態から、転てつ器の状態を決定する。表示リレー回路直接は存在せず、IsSwitchingとIsReverseの組み合わせを毎度取得する。
    /// </summary>
    /// <returns></returns>   
    public async Task SwitchingMachineControl()
    {
        // Todo: クラスのstaticにしたほうが良いかも
        var switchMoveTime = TimeSpan.FromSeconds(5);
        var switchReturnTime = TimeSpan.FromMilliseconds(500);
        // InterlockingObjectを全取得
        // Todo: 全取得しなくても良いようにする
        var interlockingObjects = (await interlockingObjectRepository.GetAllWithState())
            .ToDictionary(obj => obj.Id);
        // こいつは定常で全駅回すので駅ごとに分けるやつの対象外
        // 転てつ機を全取得
        var switchingMachineList = interlockingObjects
            .Where(x => x.Value is SwitchingMachine)
            .Select(x => x.Value as SwitchingMachine)
            .ToList();
        // 転てつ機てこを全取得
        var levers = interlockingObjects
            .Where(x => x.Value is Lever { SwitchingMachineId: not null })
            .Select(x => x.Value as Lever)
            .ToDictionary(x => x.SwitchingMachineId);
        // SwitchingMachineRouteのリストを取得
        var switchingMachineRouteDict = (await switchingMachineRouteRepository.GetAll())
            .GroupBy(x => x.SwitchingMachineId)
            .ToDictionary(x => x.Key, x => x.ToList());

        // 全進路を取得
        var routes = interlockingObjects
            .Where(x => x.Value is Route)
            .Select(x => x.Value as Route)
            .ToDictionary(x => x.Id);
        // 全てっさ鎖錠欄を取得
        var detectorLockConditions = await lockConditionRepository.GetConditionsByType(LockType.Detector);

        foreach (var switchingMachine in switchingMachineList)
        {
            // 対応する転てつ器のてっさ鎖錠欄の条件確認
            var detectorLockCondition = detectorLockConditions.GetValueOrDefault(switchingMachine.Id, []);
            if (IsDetectorLocked(detectorLockCondition, interlockingObjects))
            {
                // てっさ鎖錠領域に転換中に列車が来ると転換完了処理も通らないが、転換中の転てつ器に突っ込んだ結果転てつ器が壊れたとする。
                continue;
            }

            // 対応する転てつ器のSwitchingMachineStateを取得 
            var switchingMachineState = switchingMachine.SwitchingMachineState;
            var now = dateTimeRepository.GetNow();
            // 転換中の転てつ器が、転換を終了した場合
            if (switchingMachineState.IsSwitching && switchingMachineState.SwitchEndTime < now)
            {
                // 対応する転てつ器のSwitchingMachineState.IsSwitchingをfalseにする 
                switchingMachineState.IsSwitching = false;
                await generalRepository.Save(switchingMachineState);
            }

            // 対応する転てつ器のてこ状態を取得
            var leverState = levers[switchingMachine.Id].LeverState.IsReversed;

            var requestNormal = leverState == LCR.Left;
            var requestReverse = leverState == LCR.Right;

            // 対応する転てつ器の要求進路一覧を取得
            var switchingMachineRouteList = switchingMachineRouteDict[switchingMachine.Id];

            foreach (var switchingMachineRoute in switchingMachineRouteList)
            {
                // 対応する進路のRouteState.IsLeverRelayRaisedを取得
                var route = routes[switchingMachineRoute.RouteId];
                var isLeverRelayRaised = route.RouteState.IsLeverRelayRaised == RaiseDrop.Raise;
                if (isLeverRelayRaised)
                {
                    switch (switchingMachineRoute.IsReverse)
                    {
                        case NR.Normal:
                            requestNormal = true;
                            break;
                        case NR.Reversed:
                            requestReverse = true;
                            break;
                    }
                }
            }

            if (requestNormal == requestReverse)
            {
                //何もしない
                continue;
            }

            if (requestNormal)
            {
                // 既に定位(or定位に転換中)の場合はスキップ
                if (switchingMachineState.IsReverse == NR.Normal)
                {
                    continue;
                }

                // 対応する転てつ器のSwitchingMachineState.IsReverseをNR.Normalにする      
                switchingMachineState.IsReverse = NR.Normal;
            }
            else if (requestReverse)
            {
                // 既に反位(or反位に転換中)の場合はスキップ
                if (switchingMachineState.IsReverse == NR.Reversed)
                {
                    continue;
                }

                // 対応する転てつ器のSwitchingMachineState.IsReverseをNR.Reversedにする    
                switchingMachineState.IsReverse = NR.Reversed;
            }
            DateTime switchEndTime;
            // 転換中の転てつ器が、転換を終了する前に反転した場合
            if (switchingMachineState.IsSwitching && now < switchingMachineState.SwitchEndTime)
            {
                switchEndTime = now + (now - switchingMachineState.SwitchEndTime + switchMoveTime + switchReturnTime);
            }
            // 転換していない転てつ器が転換する場合
            else
            {
                switchEndTime = now + switchMoveTime;
            }

            // 対応する転てつ器のSwitchingMachineState.IsSwitchingをtrueにする  
            switchingMachineState.IsSwitching = true;
            // 対応する転てつ器のSwitchingMachineState.SwitchEndTimeをSwitchEndTimeにする
            switchingMachineState.SwitchEndTime = switchEndTime;
            await generalRepository.Save(switchingMachineState);
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
        // 直接鎖状条件を取得
        var directLockCondition = await lockConditionRepository.GetConditionsByType(LockType.Lock);
        // てこリレーが扛上している進路を全て取得
        var routes = interlockingObjects
            .Where(x => x.Value is Route route && route.RouteState!.IsLeverRelayRaised == RaiseDrop.Raise)
            .Select(x => (x.Value as Route)!)
            .ToList();
        // てこリレーが扛上している進路の信号制御欄を取得
        var signalControlConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            routes.Select(x => x.Id).ToList(), LockType.SignalControl);
        foreach (var route in routes)
        {
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
                    _ => false
                };
            };
            if (!EvaluateLockConditions(directLockCondition[route.Id], interlockingObjects, predicate))
            {
                continue;
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
                continue;
            }

            // 鎖錠確認 進路の鎖錠欄の条件を満たしていない場合早期continue
            if (IsLocked(directLockCondition[route.Id], interlockingObjects))
            {
                continue;
            }

            // 進路のRouteState.IsRouteRelayRaisedをRaiseにする
            route.RouteState.IsRouteRelayRaised = RaiseDrop.Raise;
            await generalRepository.Save(route.RouteState);
        }
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

        // 進路照査リレーが扛上している進路を全て取得
        var routes = interlockingObjects
            .Where(x => x.Value is Route route && route.RouteState!.IsRouteRelayRaised == RaiseDrop.Raise)
            .Select(x => (x.Value as Route)!)
            .ToList();
        // 直接鎖状条件を取得
        var directLockCondition = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            routes.Select(x => x.Id).ToList(), LockType.Lock);
        // 進路照査リレーが扛上している進路の転轍機てっさ鎖状欄を取得
        var detectorLockConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            routes.Select(x => x.Id).ToList(), LockType.Detector);

        foreach (var route in routes)
        {
            // 進路の鎖錠欄のうち転轍器のてっさ鎖錠がかかっているか
            if (IsDetectorLocked(detectorLockConditions[route.Id], interlockingObjects))
            {
                continue;
            }

            // 鎖錠確認 進路の鎖錠欄の条件を満たしていない場合早期continue
            if (IsLocked(directLockCondition[route.Id], interlockingObjects))
            {
                continue;
            }
            // Question: 鎖状確認 信号制御欄の条件を満たしているか確認?

            // 進路のRouteState.IsSignalControlRaisedをRaiseにする
            route.RouteState.IsSignalControlRaised = RaiseDrop.Raise;
            await generalRepository.Save(route.RouteState);
        }
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
                Route route => route.RouteState.IsLeverRelayRaised != RaiseDrop.Drop,
                SwitchingMachine or Route or Lever => false,
                _ => true
            };
        }
    }

    private bool IsDetectorLocked(List<LockCondition> lockConditions,
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

    /// <summary>
    /// <strong>総括制御確認</strong><br/>
    /// 総括条件をもとに1つ確認する
    /// </summary>
    /// <returns></returns>
    private bool EvaluateThrowOutCondition(Route targetRoute)
    {
        return targetRoute.RouteState.IsThrowOutXRRelayRaised == RaiseDrop.Raise && targetRoute.RouteState.IsLeverRelayRaised == RaiseDrop.Raise;
    }
}