using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Button;
using Traincrew_MultiATS_Server.Repositories.Datetime;
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
    IButtonRepository buttonRepository,
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
        var buttons = await buttonRepository.GetAllButtons();
        // 直接鎖状条件を取得
        var lockConditions = await lockConditionRepository.GetConditionsByType(LockType.Lock);

        // ここまで実行できればほぼほぼOOMしないはず
        foreach (var routeLeverDestinationButton in routeLeverDestinationButtonList)
        {
            // 対象進路 
            var route = (interlockingObjects[routeLeverDestinationButton.RouteId] as Route)!;
            var routeState = route.RouteState!;
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

            // てこが中立位置の場合
            if (leverState == LCR.Center)
            {
                // リレーが上がっている場合は下げる
                if (isLeverRelayRaised == RaiseDrop.Raise)
                {
                    routeState.IsLeverRelayRaised = RaiseDrop.Drop;
                    await generalRepository.Save(routeState);
                }

                continue;
            }

            // てこが倒れている場合
            if (leverState is LCR.Left or LCR.Right)
            {
                // すでにてこリレーが上がっている場合はスキップ
                if (isLeverRelayRaised == RaiseDrop.Raise)
                {
                    continue;
                }

                var isRaised = button.ButtonState.IsRaised;
                // 着点ボタンが押されている場合は、てこリレーを上げる
                if (isRaised == RaiseDrop.Raise)
                {
                    routeState.IsLeverRelayRaised = RaiseDrop.Raise;
                    await generalRepository.Save(routeState);
                }
            }

            //ここから仮コード　本番に残さない

            //第二段階用
            // Todo: [繋ぎ込み]RouteLeverDestinationButton.RouteIdの進路のRouteState.IsSignalControlRaisedに変化あればIsRouteRelayRaised代入

            //第一段階用
            // Todo: [繋ぎ込み]RouteLeverDestinationButton.RouteIdの進路のRouteState.IsSignalControlRaisedに変化あればisLeverRelayRaised代入

            //仮コードここまで
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
            if (IsDetectorLocked(detectorLockConditions[switchingMachine.Id], interlockingObjects))
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

            var RequestNormal = leverState == LCR.Left;
            var RequestReverse = leverState == LCR.Right;

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
                            RequestNormal = true;
                            break;
                        case NR.Reversed:
                            RequestReverse = true;
                            break;
                    }
                }
            }

            if (RequestNormal == RequestReverse)
            {
                //何もしない
                continue;
            }

            if (RequestNormal)
            {
                // 既に定位(or定位に転換中)の場合はスキップ
                if (switchingMachineState.IsReverse == NR.Normal)
                {
                    continue;
                }

                // 対応する転てつ器のSwitchingMachineState.IsReverseをNR.Normalにする      
                switchingMachineState.IsReverse = NR.Normal;
            }
            else if (RequestReverse)
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
}