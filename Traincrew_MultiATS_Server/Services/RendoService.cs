using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.DirectionRoute;
using Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.Lock;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.RouteLeverDestinationButton;
using Traincrew_MultiATS_Server.Repositories.RouteLockTrackCircuit;
using Traincrew_MultiATS_Server.Repositories.Station;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;
using Traincrew_MultiATS_Server.Repositories.ThrowOutControl;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Services;

// 方針メモ: とりあえずナイーブに、全取得して処理する
// やってみて処理が重い場合は、駅ごとに処理するか、必要な対象物のみ取得するように工夫する
// Hope: クエリ自体が重すぎて時間計算量的に死ぬってことはないと信じたい

/// <summary>
///     連動装置
/// </summary>
public class RendoService(
    IRouteRepository routeRepository,
    IRouteLeverDestinationRepository routeLeverDestinationRepository,
    IInterlockingObjectRepository interlockingObjectRepository,
    ISwitchingMachineRepository switchingMachineRepository,
    ISwitchingMachineRouteRepository switchingMachineRouteRepository,
    IStationRepository stationRepository,
    IDestinationButtonRepository destinationButtonRepository,
    ILockRepository lockRepository,
    ILockConditionRepository lockConditionRepository,
    IDateTimeRepository dateTimeRepository,
    IThrowOutControlRepository throwOutControlRepository,
    ITrackCircuitRepository trackCircuitRepository,
    IRouteLockTrackCircuitRepository routeLockTrackCircuitRepository,
    IDirectionRouteRepository directionRouteRepository,
    IDirectionSelfControlLeverRepository directionSelfControlLeverRepository,
    ILeverRepository leverRepository,
    IGeneralRepository generalRepository)
{
    /// <summary>
    /// <strong>てこ反応リレー回路</strong><br/>
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
        var routesByLevers = routeLeverDestinationButtonList
            .GroupBy(rdb => rdb.LeverId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(rdb => (interlockingObjects[rdb.RouteId] as Route)!).ToList());
        // Buttonを全取得
        var buttons = (await destinationButtonRepository.GetAllWithState())
            .ToDictionary(b => b.Name);
        // 直接鎖錠条件を取得
        var lockConditions = await lockConditionRepository.GetConditionsByType(LockType.Lock);
        // 信号制御条件を取得
        var signalControlConditions = await lockConditionRepository.GetConditionsByType(LockType.SignalControl);
        // 接近鎖錠欄を取得
        var approachLockConditions = await lockConditionRepository.GetConditionsByType(LockType.Approach);
        // 統括制御テーブルを取得
        var throwOutControls = await throwOutControlRepository.GetAll();
        // 進路IDをキーにして、統括制御「する」進路をグループ化
        var sourceThrowOutControls = throwOutControls
            .GroupBy(c => c.TargetId)
            .ToDictionary(g => g.Key, g => g.ToList());
        // 進路IDをキーにして、統括制御「される」進路をグループ化
        var targetThrowOutControls = throwOutControls
            .GroupBy(c => c.SourceId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // ここまで実行できればほぼほぼOOMしないはず
        foreach (var routeLeverDestinationButton in routeLeverDestinationButtonList)
        {
            // 対象進路 
            var route = (interlockingObjects[routeLeverDestinationButton.RouteId] as Route)!;
            var routeState = route.RouteState!;
            // この進路に対して総括制御「する」進路
            var sourceThrowOutRoutes = sourceThrowOutControls.GetValueOrDefault(route.Id, [])
                .Select(toc => interlockingObjects[toc.SourceId])
                .OfType<Route>()
                .ToList();
            var hasSourceThrowOutRoute = sourceThrowOutRoutes.Count != 0;
            // この進路に対して総括制御「される」進路
            var targetThrowOutRoutes = targetThrowOutControls.GetValueOrDefault(route.Id, [])
                .Select(toc => interlockingObjects[toc.TargetId])
                .OfType<Route>()
                .ToList();
            // targetThrowOutRoutes の TargetId をキーとする辞書を生成
            var targetSourceThrowOutRoutes = targetThrowOutRoutes
                .ToDictionary(
                    toc => toc.Id, // TargetId をキーに
                    toc => sourceThrowOutControls.GetValueOrDefault(toc.Id, []) // SourceId のリストを取得
                        .Select(toc => interlockingObjects[toc.SourceId])
                        .Distinct()
                        .OfType<Route>()
                        .Where(r => r.Id != route.Id)
                        .ToList()
                );

            // 対象てこ
            var lever = (interlockingObjects[routeLeverDestinationButton.LeverId] as Lever)!;
            // 対象ボタン
            DestinationButton button;
            if (routeLeverDestinationButton.DestinationButtonName != null)
            {
                button = buttons[routeLeverDestinationButton.DestinationButtonName];
            }
            else
            {
                button = new()
                {
                    Name = "TH99_DummyP",
                    StationId = "TH99",
                    DestinationButtonState = new() { IsRaised = RaiseDrop.Raise }
                };
            }
            // 鎖錠条件 
            var lockCondition = lockConditions.GetValueOrDefault(route.Id, []);

            // 同一のレバーを持つ自分以外の進路を取得
            var otherRoutes = routesByLevers[routeLeverDestinationButton.LeverId]
                .Where(r => r.Id != route.Id)
                .ToList();

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
                //総括する各進路の根本レバーを取得
                var sourceLevers = sourceThrowOutRoutes
                    .Select(r => routeLevers[r.Id])
                    .ToList();

                // 総括する各進路の自分に関係しない進路をすべて取得
                var sourceOtherRoutes = sourceLevers
                    .SelectMany(lever => routesByLevers[lever.Id])
                    .Where(r => !sourceThrowOutRoutes.Any(s => s.Id == r.Id))
                    .ToList();

                var isThrowOutXRRelayRaised =
                    sourceOtherRoutes.All(r => r.RouteState.IsLeverRelayRaised == RaiseDrop.Drop)
                    &&
                    //総括する各進路の根本レバーのずれかが倒れているか
                    sourceLevers.Any(l => l.LeverState.IsReversed != LCR.Center)
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
                    )
                        ? RaiseDrop.Raise
                        : RaiseDrop.Drop;
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
                var approachLockFinalTrackCircuitId = route.ApproachLockFinalTrackCircuitId;
                var finalApproachTrackState = false;
                if (approachLockFinalTrackCircuitId != null
                    && interlockingObjects.TryGetValue(approachLockFinalTrackCircuitId.Value, out var finalApproachTrackCircuit))
                {
                    var finalTrackCircuit = (finalApproachTrackCircuit as TrackCircuit)!;
                    // 自進路の接近鎖錠欄に書かれている条件のうち最終の軌道回路条件の状態を取得 鎖錠していない
                    finalApproachTrackState = !finalTrackCircuit.TrackCircuitState.IsLocked;
                }
                // 自進路の信号制御欄に書かれている条件に書かれている軌道回路の状態を取得
                var signalControlLockTrackCircuitState = signalControlConditions[route.Id]
                    .OfType<LockConditionObject>()
                    .Select(l => interlockingObjects[l.ObjectId])
                    .OfType<TrackCircuit>()
                    .Select(tc => tc.TrackCircuitState)
                    .ToList();
                var isThrowOutYSRelayRaised =
                    // 進路鎖錠実装時にコメントアウト解除した（下2行）　進路鎖錠なしで動確取る場合は再度コメントアウト
                    lockRouteStates.All(rs => rs.IsRouteLockRaised == RaiseDrop.Raise)
                    &&
                    (
                        sourceThrowOutRoutes.Any(r =>
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
                    )
                        ? RaiseDrop.Raise
                        : RaiseDrop.Drop;
                if (routeState.IsThrowOutYSRelayRaised != isThrowOutYSRelayRaised)
                {
                    isChanged = true;
                }

                routeState.IsThrowOutYSRelayRaised = isThrowOutYSRelayRaised;
            }

            var isLeverRelayRaised =
                !IsLocked(lockCondition, interlockingObjects)
                &&
                otherRoutes.All(route => route.RouteState.IsLeverRelayRaised == RaiseDrop.Drop)
                &&
                (
                    (
                        routeState is
                        { IsThrowOutYSRelayRaised: RaiseDrop.Drop, IsThrowOutXRRelayRaised: RaiseDrop.Drop }
                        &&
                        (
                            routeLeverDestinationButton.Direction == LR.Left && leverState == LCR.Left
                            ||
                            routeLeverDestinationButton.Direction == LR.Right && leverState == LCR.Right
                        )
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
                        &&
                        targetSourceThrowOutRoutes[r.Id].All(sr =>
                            sr.RouteState.IsLeverRelayRaised == RaiseDrop.Drop
                        )
                    )
                )
                ? RaiseDrop.Raise
                : RaiseDrop.Drop;
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
    /// <strong>転てつ器無し進路照査リレー回路</strong><br/>
    /// 現在の状態から、転てつ器以外の進路が確保され、TCに進路指示を出していいか決定する。
    /// </summary>
    /// <returns></returns> 
    public async Task RouteRelayWithoutSwitchingMachine()
    {
        // まず、てこ反応リレーが落下しているすべての進路の進路リレーをすべて落下させる
        await routeRepository.DropRouteRelayWithoutSwitchingMachineWhereLeverRelayIsDropped();

        // そのうえで、てこ反応リレーが扛上している進路のIDを全取得する
        var routeIds = await routeRepository.GetIdsWhereLeverRelayIsRaised();
        // てこリレーが扛上している進路の直接鎖錠条件を取得
        var directLockConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            routeIds, LockType.Lock);
        // てこリレーが扛上している進路の進路鎖錠条件を取得
        var routeLockConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            routeIds, LockType.Route);


        // 関わる全てのObjectを取得
        var objectIds = routeIds
            .Union(directLockConditions.Values.SelectMany(ExtractObjectIdsFromLockCondtions))
            .Union(routeLockConditions.Values.SelectMany(ExtractObjectIdsFromLockCondtions))
            .Distinct()
            .ToList();
        var interlockingObjects = (await interlockingObjectRepository.GetObjectByIdsWithState(objectIds))
            .ToDictionary(obj => obj.Id);

        foreach (var routeId in routeIds)
        {
            // 対象進路
            var route = (interlockingObjects[routeId] as Route)!;
            var directLockCondition = directLockConditions.GetValueOrDefault(routeId, []);
            var routeLockCondition = routeLockConditions.GetValueOrDefault(routeId, []);
            var result = ProcessRouteRelayWithoutSwitchingMachine(route, directLockCondition, routeLockCondition, interlockingObjects);
            if (route.RouteState.IsRouteRelayWithoutSwitchingMachineRaised == result)
            {
                continue;
            }
            route.RouteState.IsRouteRelayWithoutSwitchingMachineRaised = result;
            await generalRepository.Save(route.RouteState);
        }
    }

    /// <summary>
    /// <strong>進路照査リレー回路</strong><br/>
    /// 現在の状態から、進路が確保されているか決定する。
    /// </summary>
    /// <returns></returns>   
    public async Task RouteRelay()
    {
        // まず、転てつ器無し進路照査リレーが落下しているすべての進路の進路リレーをすべて落下させる
        await routeRepository.DropRouteRelayWhereRouteRelayWithoutSwitchingMachineIsDropped();

        // そのうえで、転てつ器無し進路照査リレーが扛上している進路のIDを全取得する
        var routeIds = await routeRepository.GetIdsWhereLeverRelayIsRaised();
        // 上記進路に対して統括制御「する」進路の統括制御をすべて取得
        var sourceThrowOutControlList = await throwOutControlRepository.GetByTargetIds(routeIds);
        var sourceThrowOutControlDictionary = sourceThrowOutControlList
            .GroupBy(c => c.TargetId)
            .ToDictionary(g => g.Key, g => g.ToList());
        // てこリレーが扛上している進路の直接鎖状条件を取得
        var directLockConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            routeIds, LockType.Lock);
        // てこリレーが扛上している進路の信号制御欄を取得
        var signalControlConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            routeIds, LockType.SignalControl);

        // 関わる全てのObjectを取得
        var objectIds = routeIds
            .Union(sourceThrowOutControlList.Select(c => c.SourceId))
            .Union(directLockConditions.Values.SelectMany(ExtractObjectIdsFromLockCondtions))
            .Union(signalControlConditions.Values.SelectMany(ExtractObjectIdsFromLockCondtions))
            .Distinct()
            .ToList();
        var interlockingObjects = (await interlockingObjectRepository.GetObjectByIdsWithState(objectIds))
            .ToDictionary(obj => obj.Id);
        foreach (var routeId in routeIds)
        {
            // 対象進路
            var route = (interlockingObjects[routeId] as Route)!;
            var directLockCondition = directLockConditions.GetValueOrDefault(routeId, []);
            var signalControlCondition = signalControlConditions.GetValueOrDefault(routeId, []);

            // この進路に対して総括制御「する」進路
            var sourceThrowOutRoutes = sourceThrowOutControlDictionary.GetValueOrDefault(route.Id, [])
                .Select(toc => interlockingObjects[toc.SourceId])
                .OfType<Route>()
                .ToList();

            var result = ProcessRouteRelay(route, directLockCondition, signalControlCondition, sourceThrowOutRoutes, interlockingObjects);
            if (route.RouteState.IsRouteRelayRaised == result)
            {
                continue;
            }
            route.RouteState.IsRouteRelayRaised = result;
            await generalRepository.Save(route.RouteState);
        }
    }

    private RaiseDrop ProcessRouteRelayWithoutSwitchingMachine(Route route,
        List<LockCondition> directLockCondition,
        List<LockCondition> routeLockCondition,
        Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        // てこリレーが落下している場合、進路リレーを落下させてcontinue
        if (route.RouteState.IsLeverRelayRaised == RaiseDrop.Drop)
        {
            return RaiseDrop.Drop;
        }

        // 進路の進路鎖錠欄の条件を満たしているかを取得
        if (!IsEnsuredRouteByDirectLockConditions(routeLockCondition, interlockingObjects))
        {
            return RaiseDrop.Drop;
        }

        // 進路の直接鎖錠欄の条件を確保中かを取得
        if (!IsEnsuringRouteByDirectLockConditions(directLockCondition, interlockingObjects))
        {
            return RaiseDrop.Drop;
        }
        // 進路のRouteState.IsRouteRelayWithoutSwitchingMachineRaisedをRaiseにする
        return RaiseDrop.Raise;
    }

    private RaiseDrop ProcessRouteRelay(Route route,
        List<LockCondition> directLockCondition,
        List<LockCondition> signalControlConditions,
        List<Route> sourceThrowOutRoutes,
        Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        // てこリレーが落下している場合、進路リレーを落下させてcontinue
        if (route.RouteState.IsLeverRelayRaised == RaiseDrop.Drop)
        {
            return RaiseDrop.Drop;
        }

        // 進路の鎖錠欄の条件を満たしているかを取得
        if (!IsEnsuredRouteByDirectLockConditions(directLockCondition, interlockingObjects))
        {
            return RaiseDrop.Drop;
        }

        // 進路の信号制御欄の条件を満たしているか確認  
        if (!IsEnsuredRouteBySignalControlConditions(signalControlConditions, interlockingObjects))
        {
            return RaiseDrop.Drop;
        }

        // 総括制御の条件を満たしているか確認
        if (!(
                (
                    // 自進路YS扛上
                    route.RouteState.IsThrowOutYSRelayRaised == RaiseDrop.Raise
                    // この進路に対して総括制御「する」進路の進路鎖錠リレーが落下している
                    && sourceThrowOutRoutes.Any(r => r.RouteState.IsRouteLockRaised == RaiseDrop.Drop)
                )
                // 自進路YS落下
                || route.RouteState.IsThrowOutYSRelayRaised == RaiseDrop.Drop)
           )
        {
            return RaiseDrop.Drop;
        }

        // 進路のRouteState.IsRouteRelayRaisedをRaiseにする
        return RaiseDrop.Raise;
    }

    /// <summary>
    /// 時素リレー回路
    /// </summary>
    public async Task TimerRelay()
    {
        // 接近鎖錠MSが扛上している進路を全部取ってくる
        var routes = await routeRepository.GetWhereApproachLockMSRelayIsRaised();
        // 進路の接近鎖錠MSリレーが扛上している進路が持っている駅とタイマーのセット
        var setStationAndApproachLockTime = routes
            .Select(r => (r.StationId, r.ApproachLockTime)).ToHashSet();
        // 駅の全タイマーを取ってくる
        // Todo: 全ての駅のタイマーを取ってこなくても良いようにする(必要な分だけ見る)
        var stationTimerStates = (await stationRepository.GetAllTimerStates())
            .ToDictionary(s => (s.StationId, s.Seconds));
        // ループ
        foreach (var stationTimerState in stationTimerStates.Values)
        {
            var isTeuRelayRaised = RaiseDrop.Drop;
            RaiseDrop isTenRelayRaised;
            RaiseDrop isTerRelayRaised;
            var teuRelayRaisedAt = stationTimerState.TeuRelayRaisedAt;

            // 当該駅で、接近鎖錠MSリレーが扛上している進路があるかチェック
            var hasRoute =
                setStationAndApproachLockTime.Contains((stationTimerState.StationId, stationTimerState.Seconds));

            // 扛上している進路がある場合
            if (hasRoute)
            {
                if (teuRelayRaisedAt == null)
                {
                    isTeuRelayRaised = RaiseDrop.Drop;
                    teuRelayRaisedAt = dateTimeRepository.GetNow().AddSeconds(stationTimerState.Seconds);
                }
            }
            else
            {
                isTeuRelayRaised = RaiseDrop.Drop;
                teuRelayRaisedAt = null;
            }

            if (teuRelayRaisedAt != null && teuRelayRaisedAt < dateTimeRepository.GetNow())
            {
                isTeuRelayRaised = RaiseDrop.Raise;
            }

            if (teuRelayRaisedAt != null)
            {
                // 時素リレーが上がりきった場合
                if (isTeuRelayRaised == RaiseDrop.Raise)
                {
                    isTenRelayRaised = RaiseDrop.Raise;
                    isTerRelayRaised = RaiseDrop.Drop;
                }
                // 時素リレーが上がりきってない場合
                else
                {
                    isTenRelayRaised = RaiseDrop.Drop;
                    isTerRelayRaised = RaiseDrop.Drop;
                }
            }
            // 時素リレーが落ちた時
            else
            {
                isTenRelayRaised = RaiseDrop.Drop;
                isTerRelayRaised = RaiseDrop.Raise;
            }

            // それぞれ現在と異なる場合、更新
            if (stationTimerState.IsTeuRelayRaised == isTeuRelayRaised
                && stationTimerState.IsTenRelayRaised == isTenRelayRaised
                && stationTimerState.IsTerRelayRaised == isTerRelayRaised
                && stationTimerState.TeuRelayRaisedAt == teuRelayRaisedAt)
            {
                continue;
            }

            stationTimerState.IsTeuRelayRaised = isTeuRelayRaised;
            stationTimerState.IsTenRelayRaised = isTenRelayRaised;
            stationTimerState.IsTerRelayRaised = isTerRelayRaised;
            stationTimerState.TeuRelayRaisedAt = teuRelayRaisedAt;
            await generalRepository.Save(stationTimerState);
        }
    }

    /// <summary>
    /// <strong>方向てこリレー回路</strong><br/>
    /// </summary>
    /// <returns></returns>
    public async Task DirectionRelay()
    {
        // 方向進路の全取得
        var directionRouteIds = await directionRouteRepository.GetAllIds();
        // 方向てこの全取得
        var directionLeverIds = await leverRepository.GetAllIds();
        // 直接鎖錠条件を全取得
        var lockConditionDict = await lockConditionRepository.GetConditionsByType(LockType.Lock);
        // 開放てこの全ID取得
        var directionSelfControlLeverIds = await directionSelfControlLeverRepository.GetAllIds();

        // 総括制御を全取得
        // Todo: 方向進路関係のものだけ取得するようにする
        var throwOutControls = await throwOutControlRepository.GetAll();

        // 総括されるオブジェクトをキーとして総括制御をグループ化
        var targetThrowOutControls = throwOutControls
            .GroupBy(c => c.TargetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 関わる全てのObjectを取得
        var objectIds = directionRouteIds
            .Union(directionLeverIds)
            .Union(directionSelfControlLeverIds)
            .Union(throwOutControls.Select(toc => toc.SourceId))
            .Union(throwOutControls.Select(toc => toc.ConditionLeverId).OfType<ulong>())
            .Union(lockConditionDict.Values.SelectMany(ExtractObjectIdsFromLockCondtions))
            .ToList();
        var interlockingObjects = (await interlockingObjectRepository.GetObjectByIdsWithState(objectIds))
            .ToDictionary(obj => obj.Id);
        var directionSelfControlLevers = interlockingObjects
            .Where(kvp => kvp.Value is DirectionSelfControlLever)
            .ToDictionary(kvp => kvp.Key, kvp => (kvp.Value as DirectionSelfControlLever)!);

        // 方向てこごとにループ
        foreach (var directionRouteId in directionRouteIds)
        {
            // DB制約的にDirectionRouteになるはず
            var directionRoute = (interlockingObjects[directionRouteId] as DirectionRoute)!;
            var directionRouteState = directionRoute.DirectionRouteState;

            var physicalLever = (interlockingObjects[directionRoute.LeverId] as Lever)!;
            var directionSelfControlLever = directionSelfControlLevers[directionRoute.DirectionSelfControlLeverId.Value];

            // 運転方向鎖錠リレー    
            // 対応する鎖錠軌道回路を取得
            var lockTrackCircuits = lockConditionDict[directionRouteId]
                .OfType<LockConditionObject>()
                .Select(lco => interlockingObjects[lco.ObjectId])
                .OfType<TrackCircuit>();


            // 総括される進路を各方向ごとに取得
            var thisLeftThrowOutControls = targetThrowOutControls[directionRoute.Id]
                .Where(t => t.TargetLr == LR.Left)
                .ToList();
            var thisRightThrowOutControls = targetThrowOutControls[directionRoute.Id]
                .Where(t => t.TargetLr == LR.Right)
                .ToList();


            // 各進路のてこ反応リレーが落下していて、進路鎖錠リレーが扛上している場合true
            bool PredicateIsFlRelayRaised(ThrowOutControl t) =>
                (interlockingObjects[t.SourceId] as Route).RouteState.IsLeverRelayRaised == RaiseDrop.Drop
                && (interlockingObjects[t.SourceId] as Route).RouteState.IsRouteLockRaised == RaiseDrop.Raise;

            // 対応軌道回路の短絡状態によって、FLリレーを扛上
            var isFLRelayRaised = lockTrackCircuits.All(tc => !tc.TrackCircuitState.IsShortCircuit)
                && thisLeftThrowOutControls.All(PredicateIsFlRelayRaised)
                && thisRightThrowOutControls.All(PredicateIsFlRelayRaised)
                ? RaiseDrop.Raise
                : RaiseDrop.Drop;

            // 総括リレー      
            // thisLeftThrowOutControlsのSourceの進路のてこ反応リレー扛上と、ConditionLeverの状態を確認する
            bool PredicateIsYsRelayRaised(ThrowOutControl t) =>
                (interlockingObjects[t.SourceId] as Route).RouteState.IsLeverRelayRaised == RaiseDrop.Raise
                && t.ConditionLeverId.HasValue
                && directionSelfControlLevers[t.ConditionLeverId.Value].DirectionSelfControlLeverState
                    .IsReversed == NR.Normal;

            var isLfysRelayRaised = thisLeftThrowOutControls
                .Any(PredicateIsYsRelayRaised)
                ? RaiseDrop.Raise
                : RaiseDrop.Drop;

            var isRfysRelayRaised = thisRightThrowOutControls
                .Any(PredicateIsYsRelayRaised)
                ? RaiseDrop.Raise
                : RaiseDrop.Drop;


            // てこ反応リレー
            // 対応する総括リレー扛上
            // ||
            // (
            //     対応する開放てこR位置
            //     &&
            //     レバー本体がその向きかどうか
            // )
            // => Trueで向上
            var isLyRelayRaised =
                isLfysRelayRaised == RaiseDrop.Raise
                ||
                (
                    directionSelfControlLever.DirectionSelfControlLeverState.IsReversed == NR.Reversed
                    &&
                    physicalLever.LeverState.IsReversed == LCR.Left
                )
                ||
                (
                    directionRouteState.IsRyRelayRaised == RaiseDrop.Drop
                    &&
                    directionRouteState.IsLyRelayRaised == RaiseDrop.Raise
                )
                    ? RaiseDrop.Raise
                    : RaiseDrop.Drop;
            var isRyRelayRaised =
                isRfysRelayRaised == RaiseDrop.Raise
                ||
                (
                    directionSelfControlLever.DirectionSelfControlLeverState.IsReversed == NR.Reversed
                    &&
                    physicalLever.LeverState.IsReversed == LCR.Right
                )
                ||
                (
                    directionRouteState.IsLyRelayRaised == RaiseDrop.Drop
                    &&
                    directionRouteState.IsRyRelayRaised == RaiseDrop.Raise
                )
                    ? RaiseDrop.Raise
                    : RaiseDrop.Drop;
            // 逆てこ反の動作確認のため、L方向2重確認
            isLyRelayRaised =
                isLfysRelayRaised == RaiseDrop.Raise
                ||
                (
                    directionSelfControlLever.DirectionSelfControlLeverState.IsReversed == NR.Reversed
                    &&
                    physicalLever.LeverState.IsReversed == LCR.Left
                )
                ||
                (
                    directionRouteState.IsRyRelayRaised == RaiseDrop.Drop
                    &&
                    directionRouteState.IsLyRelayRaised == RaiseDrop.Raise
                )
                    ? RaiseDrop.Raise
                    : RaiseDrop.Drop;
            // 方向リレー
            // 隣駅鎖錠の方向リレー扛上[非存在True]
            // &&
            // (
            //      (
            //          反応リレー扛上
            //          &&
            //          運転方向鎖錠リレー扛上
            //      )
            //      ||
            //      隣駅被片鎖錠の方向リレー扛上[非存在False]
            //      ||
            //      (
            //          運転方向鎖錠リレー落下
            //          &&
            //          自身扛上
            //      )
            //  )
            //  &&
            //  向き反対の方向リレー落下
            //  => Trueで向上
            var LLockLeverState = RaiseDrop.Raise;
            if (directionRoute.LLockLeverId.HasValue)
            {
                if (directionRoute.LLockLeverDirection == LR.Left)
                {
                    LLockLeverState = (interlockingObjects[directionRoute.LLockLeverId.Value] as DirectionRoute)
                        .DirectionRouteState.IsLRelayRaised;
                }
                else if (directionRoute.LLockLeverDirection == LR.Right)
                {
                    LLockLeverState = (interlockingObjects[directionRoute.LLockLeverId.Value] as DirectionRoute)
                        .DirectionRouteState.IsRRelayRaised;
                }
                else
                {
                    LLockLeverState = RaiseDrop.Drop;
                }
            }

            var LSingleLockedLeverIdState = RaiseDrop.Drop;
            if (directionRoute.LSingleLockedLeverId.HasValue)
            {
                if (directionRoute.LSingleLockedLeverDirection == LR.Left)
                {
                    LSingleLockedLeverIdState = (interlockingObjects[directionRoute.LSingleLockedLeverId.Value] as DirectionRoute)
                        .DirectionRouteState.IsLRelayRaised;
                }
                else if (directionRoute.LSingleLockedLeverDirection == LR.Right)
                {
                    LSingleLockedLeverIdState = (interlockingObjects[directionRoute.LSingleLockedLeverId.Value] as DirectionRoute)
                        .DirectionRouteState.IsRRelayRaised;
                }
                else
                {
                    LLockLeverState = RaiseDrop.Drop;
                }
            }

            var RLockLeverState = RaiseDrop.Raise;
            if (directionRoute.RLockLeverId.HasValue)
            {
                if (directionRoute.RLockLeverDirection == LR.Left)
                {
                    RLockLeverState = (interlockingObjects[directionRoute.RLockLeverId.Value] as DirectionRoute)
                        .DirectionRouteState.IsLRelayRaised;
                }
                else if (directionRoute.RLockLeverDirection == LR.Right)
                {
                    RLockLeverState = (interlockingObjects[directionRoute.RLockLeverId.Value] as DirectionRoute)
                        .DirectionRouteState.IsRRelayRaised;
                }
                else
                {
                    RLockLeverState = RaiseDrop.Drop;
                }
            }

            var RSingleLockedLeverIdState = RaiseDrop.Drop;
            if (directionRoute.RSingleLockedLeverId.HasValue)
            {
                if (directionRoute.RSingleLockedLeverDirection == LR.Left)
                {
                    RSingleLockedLeverIdState = (interlockingObjects[directionRoute.RSingleLockedLeverId.Value] as DirectionRoute)
                        .DirectionRouteState.IsLRelayRaised;
                }
                else if (directionRoute.RSingleLockedLeverDirection == LR.Right)
                {
                    RSingleLockedLeverIdState = (interlockingObjects[directionRoute.RSingleLockedLeverId.Value] as DirectionRoute)
                        .DirectionRouteState.IsRRelayRaised;
                }
                else
                {
                    RSingleLockedLeverIdState = RaiseDrop.Drop;
                }
            }

            var isLRelayRaised =
                (RLockLeverState == RaiseDrop.Drop || !directionRoute.LSingleLockedLeverId.HasValue)
                &&
                LLockLeverState == RaiseDrop.Raise
                &&
                (
                    (
                        isLyRelayRaised == RaiseDrop.Raise
                        &&
                        isFLRelayRaised == RaiseDrop.Raise
                    )
                    ||
                    LSingleLockedLeverIdState == RaiseDrop.Raise
                    ||
                    (
                        isFLRelayRaised == RaiseDrop.Drop
                        &&
                        directionRouteState.IsLRelayRaised == RaiseDrop.Raise
                    )
                )
                &&
                directionRouteState.IsRRelayRaised == RaiseDrop.Drop
                    ? RaiseDrop.Raise
                    : RaiseDrop.Drop;

            var isRRelayRaised =
                (LLockLeverState == RaiseDrop.Drop || !directionRoute.RSingleLockedLeverId.HasValue)
                &&
                RLockLeverState == RaiseDrop.Raise
                &&
                (
                    (
                        isRyRelayRaised == RaiseDrop.Raise
                        &&
                        isFLRelayRaised == RaiseDrop.Raise
                    )
                    ||
                    RSingleLockedLeverIdState == RaiseDrop.Raise
                    ||
                    (
                        isFLRelayRaised == RaiseDrop.Drop
                        &&
                        directionRouteState.IsRRelayRaised == RaiseDrop.Raise
                    )
                )
                &&
                directionRouteState.IsLRelayRaised == RaiseDrop.Drop
                    ? RaiseDrop.Raise
                    : RaiseDrop.Drop;

            var isLr = directionRouteState.isLr;
            if (isLRelayRaised == RaiseDrop.Raise)
            {
                isLr = LR.Left;
            }
            else if (isRRelayRaised == RaiseDrop.Raise)
            {
                isLr = LR.Right;
            }

            // それぞれ現在と異なる場合、更新
            if (isLr == directionRouteState.isLr &&
                isFLRelayRaised == directionRouteState.IsFlRelayRaised &&
                isLfysRelayRaised == directionRouteState.IsLfysRelayRaised &&
                isRfysRelayRaised == directionRouteState.IsRfysRelayRaised &&
                isLyRelayRaised == directionRouteState.IsLyRelayRaised &&
                isRyRelayRaised == directionRouteState.IsRyRelayRaised &&
                isLRelayRaised == directionRouteState.IsLRelayRaised &&
                isRRelayRaised == directionRouteState.IsRRelayRaised)
            {
                continue;
            }

            directionRouteState.isLr = isLr;
            directionRouteState.IsFlRelayRaised = isFLRelayRaised;
            directionRouteState.IsLfysRelayRaised = isLfysRelayRaised;
            directionRouteState.IsRfysRelayRaised = isRfysRelayRaised;
            directionRouteState.IsLyRelayRaised = isLyRelayRaised;
            directionRouteState.IsRyRelayRaised = isRyRelayRaised;
            directionRouteState.IsLRelayRaised = isLRelayRaised;
            directionRouteState.IsRRelayRaised = isRRelayRaised;
            await generalRepository.Save(directionRouteState);
        }
    }


    // Todo: 接近鎖錠リレー回路     
    public async Task ApproachLockRelay()
    {
        // 操作対象の進路を全て取得(進路照査が扛上している進路または接近鎖錠の掛かっている進路)
        var routeIds = await routeRepository.GetIdsForApproachLockRelay();

        // 接近鎖錠条件を取得
        var approachLockConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            routeIds, LockType.Approach);
        // 進路鎖錠するべき軌道回路IDを取得
        var routeLockTrackCircuitList = await routeLockTrackCircuitRepository.GetByRouteIds(routeIds);

        // 関わる全てのObjectを取得 
        var objectIds = routeIds
            .Union(approachLockConditions.Values.SelectMany(ExtractObjectIdsFromLockCondtions))
            .Union(routeLockTrackCircuitList.Select(rltc => rltc.TrackCircuitId))
            .Distinct()
            .ToList();
        var interlockingObjects = (await interlockingObjectRepository.GetObjectByIdsWithState(objectIds))
            .ToDictionary(obj => obj.Id);
        // 対象進路リスト
        var routes = routeIds
            .Select(routeId => interlockingObjects[routeId])
            .OfType<Route>()
            .ToList();
        // 必要な時素を取得
        var stationIds = routes
            .Select(r => r.StationId)
            .OfType<string>()
            .Distinct()
            .ToList();
        var stationTimerStates = (await stationRepository
                .GetTimerStatesByStationIds(stationIds))
            .ToDictionary(s => (s.StationId, s.Seconds));
        // 進路鎖錠するべき軌道回路リスト
        var routeLockTrackCircuits = routeLockTrackCircuitList
            .GroupBy(rltc => rltc.RouteId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(rltc => interlockingObjects[rltc.TrackCircuitId])
                    .OfType<TrackCircuit>()
                    .ToList());
        foreach (var route in routes)
        {
            // 接近鎖錠条件
            var approachLockCondition = approachLockConditions.GetValueOrDefault(route.Id, []);

            // 接近鎖錠欄の接近区間の条件を満たしているか
            var approachLockPlaceState = !ShouldApproachLock(approachLockCondition, interlockingObjects);

            // 対応する時素を取得
            StationTimerState? stationTimerState = null;
            if (route.ApproachLockTime != null)
            {
                stationTimerState =
                    stationTimerStates.GetValueOrDefault((route.StationId, route.ApproachLockTime.Value));
            }

            // もし取得できなければ、一旦デフォルト値を設定
            if (stationTimerState == null)
            {
                stationTimerState = new()
                {
                    IsTenRelayRaised = RaiseDrop.Drop,
                    IsTerRelayRaised = RaiseDrop.Drop,
                    IsTeuRelayRaised = RaiseDrop.Drop,
                    Seconds = 0,
                    StationId = route.StationId,
                };
            }

            // 内方2回路分の軌道回路が短絡しているかどうか(直列) => 進路鎖錠するべき軌道回路リストの先頭２つ
            // ・1軌道回路しかない場合は、その軌道回路が短絡しているかどうか
            // ※軌道回路条件はB付リレーを使用
            var inTwoTrackCircuitState = routeLockTrackCircuits.GetValueOrDefault(route.Id, [])
                .Take(2)
                .Any(trackCircuit => trackCircuit.TrackCircuitState.IsShortCircuit)
                ? RaiseDrop.Drop
                : RaiseDrop.Raise;

            // ※軌道回路条件はB付リレーを使用
            var inOneTrackCircuitState = routeLockTrackCircuits.GetValueOrDefault(route.Id, [])
                .Take(1)
                .Any(trackCircuit => trackCircuit.TrackCircuitState.IsShortCircuit)
                ? RaiseDrop.Drop
                : RaiseDrop.Raise;

            // Todo:停電対応
            var isApproachLockMRRaised =
                route.RouteState.IsSignalControlRaised == RaiseDrop.Drop
                &&
                route.RouteState.IsRouteRelayRaised == RaiseDrop.Drop
                &&
                (
                    approachLockPlaceState
                    ||
                    inTwoTrackCircuitState == RaiseDrop.Drop
                    ||
                    (stationTimerState.IsTenRelayRaised == RaiseDrop.Raise
                     && route.RouteState.IsApproachLockMSRaised == RaiseDrop.Raise)
                    ||
                    route.RouteState.IsApproachLockMRRaised == RaiseDrop.Raise
                )
                    ? RaiseDrop.Raise
                    : RaiseDrop.Drop;

            var isApproachLockMSRaised =
                route.RouteState.IsSignalControlRaised == RaiseDrop.Drop
                &&
                route.RouteState.IsRouteRelayRaised == RaiseDrop.Drop
                &&
                isApproachLockMRRaised == RaiseDrop.Drop
                &&
                inOneTrackCircuitState == RaiseDrop.Raise
                &&
                (
                    stationTimerState.IsTerRelayRaised == RaiseDrop.Raise
                    ||
                    route.RouteState.IsApproachLockMSRaised == RaiseDrop.Raise
                )
                    ? RaiseDrop.Raise
                    : RaiseDrop.Drop;

            //　それぞれ現在と異なる場合、更新       
            if (route.RouteState.IsApproachLockMSRaised == isApproachLockMSRaised
                && route.RouteState.IsApproachLockMRRaised == isApproachLockMRRaised)
            {
                continue;
            }

            route.RouteState.IsApproachLockMSRaised = isApproachLockMSRaised;
            route.RouteState.IsApproachLockMRRaised = isApproachLockMRRaised;
            await generalRepository.Save(route.RouteState);
        }
    }

    /// <summary>
    /// <strong>進路鎖錠リレー回路</strong><br/>
    /// 現在の状態から、軌道回路を鎖錠するか決定する。
    /// </summary>
    /// <returns></returns>
    public async Task RouteLockRelay()
    {
        // 操作対象の進路を全て取得(進路鎖錠が掛かっている回路または接近鎖錠の掛かっている回路)
        var routeIds = await routeRepository.GetIdsForRouteLockRelay();
        // 進路鎖錠欄を取得
        var routeLockConditions = await lockRepository.GetByObjectIdsAndType(
            routeIds, LockType.Route);
        // 接近鎖錠欄を取得
        var approachLockConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            routeIds, LockType.Approach);
        // 進路鎖錠するべき軌道回路IDを取得
        var routeLockTrackCircuitList = await routeLockTrackCircuitRepository.GetByRouteIds(routeIds);
        // 関わる全てのObjectを取得
        var objectIds = routeIds
            .Union(routeLockConditions.Values
                .SelectMany(locks => locks.Select(l => l.LockConditions))
                .SelectMany(ExtractObjectIdsFromLockCondtions))
            .Union(approachLockConditions.Values.SelectMany(ExtractObjectIdsFromLockCondtions))
            .Union(routeLockTrackCircuitList.Select(rltc => rltc.TrackCircuitId))
            .Distinct()
            .ToList();
        var interlockingObjects = (await interlockingObjectRepository.GetObjectByIdsWithState(objectIds))
            .ToDictionary(obj => obj.Id);
        var routeLockTrackCircuits = routeLockTrackCircuitList
            .GroupBy(rltc => rltc.RouteId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(rltc => interlockingObjects[rltc.TrackCircuitId])
                    .OfType<TrackCircuit>()
                    .ToList());

        foreach (var routeId in routeIds)
        {
            // 対象進路
            var route = (interlockingObjects[routeId] as Route)!;
            // 進路鎖錠欄
            var routeLocks = routeLockConditions.GetValueOrDefault(route.Id, []);
            // 進路鎖錠するべき軌道回路リスト
            var routeLockTrackCircuit = routeLockTrackCircuits.GetValueOrDefault(route.Id, []);

            // 進路鎖錠取るべきリストの末端回路が鎖錠されていない && 接近鎖錠されている && 進路鎖錠されていない → 一斉に軌道回路を鎖錠、進路鎖錠する
            if (!(routeLockTrackCircuit.LastOrDefault()?.TrackCircuitState.IsLocked ?? true)
                && route.RouteState.IsApproachLockMRRaised == RaiseDrop.Drop
                && route.RouteState.IsRouteLockRaised == RaiseDrop.Raise)
            {
                //  一斉に軌道回路を鎖錠、進路鎖錠する
                // 軌道回路Lock
                routeLockTrackCircuit.ForEach(tc =>
                {
                    tc.TrackCircuitState.IsLocked = true;
                    tc.TrackCircuitState.LockedBy = route.Id;
                });
                await generalRepository.SaveAll(routeLockTrackCircuit.Select(tc => tc.TrackCircuitState));
                // IsRouteLockRaisedをDropにする
                route.RouteState.IsRouteLockRaised = RaiseDrop.Drop;
                await generalRepository.Save(route.RouteState);
            }

            // 接近鎖錠が扛上して、かつ進路鎖錠リレーが落下している場合
            if (
                route.RouteState.IsApproachLockMRRaised == RaiseDrop.Raise
                && route.RouteState.IsRouteLockRaised == RaiseDrop.Drop)
            {
                // 各進路鎖錠区切りごとに、前の軌道回路が鎖錠されていない && 自軌道回路全てが短絡されていない → 当該軌道回路を解錠する 
                // 区切りに対して時間条件が存在する場合、前の軌道回路が解錠された瞬間+既定秒数をUnlockedAtに記録し、UnlockedAtを過ぎたら解錠、解錠されたらUnlockedAtをnullにする    

                foreach (var routeLockGroup in routeLocks.GroupBy(l => l.RouteLockGroup).OrderBy(g => g.Key))
                {
                    var routeLockCondition = routeLockGroup.SelectMany(l => l.LockConditions).ToList();
                    var targetTrackCircuits = routeLockCondition
                        .OfType<LockConditionObject>()
                        .Select(l => interlockingObjects[l.ObjectId])
                        .OfType<TrackCircuit>()
                        .ToList();
                    // 当該軌道回路を自分が鎖状していない(=全部解錠されている or 他進路が鎖状している)場合、スキップ
                    if (targetTrackCircuits.All(tc => tc.TrackCircuitState.LockedBy != route.Id))
                    {
                        continue;
                    }

                    // 自軌道回路いずれかが短絡している
                    if (targetTrackCircuits.Any(tc => tc.TrackCircuitState.IsShortCircuit))
                    {
                        // 時間条件があれば取得する
                        var timerSeconds = routeLockCondition
                            .OfType<LockConditionObject>()
                            .FirstOrDefault(lco => lco.TimerSeconds != null)
                            ?.TimerSeconds;
                        // 時間条件がなければbreak
                        if (timerSeconds == null)
                        {
                            break;
                        }

                        // 時間条件が存在し、UnlockedAtがnull => now+時間条件をUnlockedAtに設定してBreak
                        if (targetTrackCircuits.Any(tc => tc.TrackCircuitState.UnlockedAt == null))
                        {
                            targetTrackCircuits.ForEach(tc => tc.TrackCircuitState.UnlockedAt =
                                dateTimeRepository.GetNow() +
                                TimeSpan.FromSeconds(timerSeconds.Value));
                            await generalRepository.SaveAll(targetTrackCircuits.Select(tc => tc.TrackCircuitState));
                            break;
                        }

                        // 時間条件が存在し、UnlockedAt > now => Break
                        if (targetTrackCircuits.Any(tc =>
                                tc.TrackCircuitState.UnlockedAt > dateTimeRepository.GetNow()))
                        {
                            break;
                        }
                    }

                    // 対象軌道回路を解錠、UnlockedAtをnullにする
                    targetTrackCircuits.ForEach(tc =>
                    {
                        tc.TrackCircuitState.IsLocked = false;
                        tc.TrackCircuitState.LockedBy = null;
                        tc.TrackCircuitState.UnlockedAt = null;
                    });
                    await generalRepository.SaveAll(targetTrackCircuits.Select(tc => tc.TrackCircuitState));
                }

                // 進路鎖錠欄に書かれている軌道回路のすべての軌道回路が鎖錠解除された場合、進路鎖錠リレーを扛上させる+進路鎖錠するべき軌道回路のリストを全解除する

                // 進路鎖錠欄に書かれている軌道回路のいずれかの軌道回路が自分によって鎖状されている場合、スキップ
                if (routeLocks
                        .SelectMany(l => l.LockConditions)
                        .OfType<LockConditionObject>()
                        .Select(l => interlockingObjects[l.ObjectId])
                        .Any(io => io is TrackCircuit tc && tc.TrackCircuitState.LockedBy == route.Id))
                {
                    continue;
                }

                // 進路鎖錠するべき軌道回路のうち、自分によって鎖状されている軌道回路を全解除する
                var toUnlockedTrackCircuits = routeLockTrackCircuit
                    .Where(tc => tc.TrackCircuitState.LockedBy == route.Id)
                    .ToList();
                toUnlockedTrackCircuits.ForEach(tc =>
                {
                    tc.TrackCircuitState.IsLocked = false;
                    tc.TrackCircuitState.LockedBy = null;
                    tc.TrackCircuitState.UnlockedAt = null;
                });
                if (toUnlockedTrackCircuits.Count > 0)
                {
                    await generalRepository.SaveAll(toUnlockedTrackCircuits.Select(tc => tc.TrackCircuitState));
                }
                // 進路鎖錠リレーを扛上させる
                route.RouteState.IsRouteLockRaised = RaiseDrop.Raise;
                await generalRepository.Save(route.RouteState);
            }
        }
    }

    /// <summary>
    /// <strong>信号制御リレー回路</strong><br/>
    /// 現在の状態から、進行を指示する信号を現示してよいか決定する。
    /// </summary>
    /// <returns></returns>
    public async Task SignalControl()
    {
        // まず、進路照査リレーが落下しているすべての進路の信号制御リレーをすべて落下させる
        await routeRepository.DropSignalRelayWhereRouteRelayIsDropped();

        // そのうえで、進路照査リレーが扛上している進路のIDを全取得する
        var routeIds = await routeRepository.GetIdsWhereRouteRelayIsRaised();
        // 直接鎖状条件を取得
        var directLockConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            routeIds, LockType.Lock);
        // 信号制御欄を取得
        var signalControlConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(
            routeIds, LockType.SignalControl);
        // 進路鎖錠するべき軌道回路IDを取得
        var routeLockTrackCircuitList = await routeLockTrackCircuitRepository.GetByRouteIds(routeIds);

        // 関わる全てのObjectを取得
        var objectIds = routeIds
            .Union(directLockConditions.Values.SelectMany(ExtractObjectIdsFromLockCondtions))
            .Union(signalControlConditions.Values.SelectMany(ExtractObjectIdsFromLockCondtions))
            .Union(routeLockTrackCircuitList.Select(rltc => rltc.TrackCircuitId))
            .Distinct()
            .ToList();
        var interlockingObjects = (await interlockingObjectRepository.GetObjectByIdsWithState(objectIds))
            .ToDictionary(obj => obj.Id);
        var routeLockTrackCircuitDictionary = routeLockTrackCircuitList
            .GroupBy(rltc => rltc.RouteId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(rltc => interlockingObjects[rltc.TrackCircuitId])
                    .OfType<TrackCircuit>()
                    .ToList());

        // Forget: 進路が定反を転換する転てつ器のてっさ鎖錠が落下している(進路照査リレーでみているため)

        foreach (var routeId in routeIds)
        {
            // 対象進路
            var route = (interlockingObjects[routeId] as Route)!;
            // 直接鎖錠条件
            var directLockCondition = directLockConditions.GetValueOrDefault(routeId, []);
            // 信号制御条件
            var signalControlCondition = signalControlConditions.GetValueOrDefault(routeId, []);
            // 進路鎖錠するべき軌道回路リスト
            var routeLockTrackCircuit = routeLockTrackCircuitDictionary.GetValueOrDefault(routeId, []);
            var result = ProcessSignalControl(route, directLockCondition, signalControlCondition,
                routeLockTrackCircuit, interlockingObjects);
            // 変更があれば、DBに保存する
            if (route.RouteState.IsSignalControlRaised == result)
            {
                continue;
            }

            route.RouteState.IsSignalControlRaised = result;
            await generalRepository.Save(route.RouteState);
        }
    }

    private RaiseDrop ProcessSignalControl(Route route,
        List<LockCondition> directLockCondition,
        List<LockCondition> signalControlCondition,
        List<TrackCircuit> routeLockTrackCircuit,
        Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        // 進路照査リレーが落下している場合、信号制御リレーを落下させる
        if (route.RouteState.IsRouteRelayRaised == RaiseDrop.Drop)
        {
            return RaiseDrop.Drop;
        }

        // 接近鎖錠リレーが向上している場合、信号制御リレーを落下させる
        if (route.RouteState.IsApproachLockMRRaised == RaiseDrop.Raise ||
            route.RouteState.IsApproachLockMSRaised == RaiseDrop.Raise)
        {
            return RaiseDrop.Drop;
        }

        // 進路鎖錠リレーが向上している場合、信号制御リレーを落下させる
        if (route.RouteState.IsRouteLockRaised == RaiseDrop.Raise)
        {
            return RaiseDrop.Drop;
        }

        // 進路鎖錠するべき軌道回路のいずれかが鎖状されていない場合、信号制御リレーを落下させる
        // Todo: 自分によって鎖状されているかどうか確認する
        if (routeLockTrackCircuit.Any(tc => !tc.TrackCircuitState.IsLocked))
        {
            return RaiseDrop.Drop;
        }

        // 信号制御欄の条件を満たしていない場合早期continue
        if (MustIndicateStopSignalBySignalControlConditions(signalControlCondition, interlockingObjects))
        {
            return RaiseDrop.Drop;
        }

        // 鎖錠確認 進路の鎖錠欄の条件を満たしていない場合早期continue
        if (MustIndicateStopSignalByDirectLockConditions(directLockCondition, interlockingObjects))
        {
            return RaiseDrop.Drop;
        }

        // 進路のRouteState.IsSignalControlRaisedを扛上させる
        return RaiseDrop.Raise;
    }

    /// <summary>
    /// 進路の鎖錠欄から、進路が鎖錠されているか確認する 
    /// </summary>
    /// <param name="lockConditions"></param>
    /// <param name="interlockingObjects"></param>
    /// <returns></returns>
    private static bool IsLocked(List<LockCondition> lockConditions,
        Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        // 対象が進路のものに限る
        // 対象進路のisLeverRelayRaisedがすべてDropであることを確認する
        return !EvaluateLockConditions(lockConditions, interlockingObjects, IsLockedPredicate);
    }

    private static bool IsLockedPredicate(LockConditionObject o, InterlockingObject interlockingObject)
    {
        return interlockingObject switch
        {
            // 進路のてこ反応リレーが落下していること
            Route route => route.RouteState.IsLeverRelayRaised == RaiseDrop.Drop,
            // 軌道回路が短絡してないこと
            TrackCircuit trackCircuit => !trackCircuit.TrackCircuitState.IsShortCircuit,
            // 転てつ器は鎖錠欄通りの転換命令が出れば良いので、ここでは確認しなくてOK
            SwitchingMachine or DirectionRoute => true,
            Lever or _ => false
        };
    }

    /// <summary>
    /// 転てつ器のてっ査鎖錠欄から、転てつ器がてっ査鎖錠されているか確認する
    /// </summary>
    internal static bool IsDetectorLocked(List<LockCondition> lockConditions,
        Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        return !EvaluateLockConditions(lockConditions, interlockingObjects, IsDetectorLockedPredicate);
    }

    private static bool IsDetectorLockedPredicate(LockConditionObject o, InterlockingObject interlockingObject)
    {
        // 軌道回路は短絡していないか、同時に鎖錠されていないか
        return interlockingObject switch
        {
            TrackCircuit trackCircuit => trackCircuit.TrackCircuitState is { IsShortCircuit: false, IsLocked: false },
            _ => false,
        };
    }

    /// <summary>
    /// 進路の鎖錠欄から、進路が確保されているか確認する
    /// </summary>
    private static bool IsEnsuredRouteByDirectLockConditions(
        List<LockCondition> lockConditions,
        Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        return EvaluateLockConditions(lockConditions, interlockingObjects,
            IsEnsuredRouteByDirectLockConditionsPredicate);
    }

    private static bool IsEnsuredRouteByDirectLockConditionsPredicate(
        LockConditionObject o, InterlockingObject interlockingObject)
    {
        // 鎖錠欄転てつ器条件　転換中でなく、向きがあっている
        // 鎖錠欄進路条件　
        return interlockingObject switch
        {
            // 転てつ器表示灯がどうか→転換中でなく、向きが合っている
            SwitchingMachine switchingMachine =>
                !switchingMachine.SwitchingMachineState.IsSwitching
                && switchingMachine.SwitchingMachineState.IsReverse == o.IsReverse,
            // 接近鎖錠と進路鎖錠リレー扛上かどうか
            Route route => route.RouteState is
            { IsApproachLockMRRaised: RaiseDrop.Raise, IsRouteLockRaised: RaiseDrop.Raise },
            // 軌道回路が短絡していないこと
            TrackCircuit trackCircuit => !trackCircuit.TrackCircuitState.IsShortCircuit,
            DirectionRoute directionRoute => o.IsLR == LR.Left ? directionRoute.DirectionRouteState.IsLRelayRaised == RaiseDrop.Raise : directionRoute.DirectionRouteState.IsRRelayRaised == RaiseDrop.Raise,
            _ => false
        };
    }

    /// <summary>
    /// 進路の鎖錠欄から、進路が確保中か確認する
    /// </summary>
    private static bool IsEnsuringRouteByDirectLockConditions(
        List<LockCondition> lockConditions,
        Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        return EvaluateLockConditions(lockConditions, interlockingObjects,
            IsEnsuringRouteByDirectLockConditionsPredicate);
    }

    private static bool IsEnsuringRouteByDirectLockConditionsPredicate(
        LockConditionObject o, InterlockingObject interlockingObject)
    {
        // 鎖錠欄転てつ器条件　転換中でなく、向きがあっている
        // 鎖錠欄進路条件　
        return interlockingObject switch
        {
            // 転てつ器表示灯がどうか→向きが合っている
            SwitchingMachine switchingMachine => switchingMachine.SwitchingMachineState.IsReverse == o.IsReverse,
            // 接近鎖錠と進路鎖錠リレー扛上かどうか
            Route route => route.RouteState is
            { IsApproachLockMRRaised: RaiseDrop.Raise, IsRouteLockRaised: RaiseDrop.Raise },
            // 軌道回路が短絡していないこと
            TrackCircuit trackCircuit => !trackCircuit.TrackCircuitState.IsShortCircuit,
            DirectionRoute directionRoute => o.IsLR == LR.Left ? directionRoute.DirectionRouteState.IsLRelayRaised == RaiseDrop.Raise : directionRoute.DirectionRouteState.IsRRelayRaised == RaiseDrop.Raise,
            _ => false
        };
    }

    /// <summary>
    /// 進路の信号制御欄から、進路が確保されているか確認する
    /// </summary>
    private static bool IsEnsuredRouteBySignalControlConditions(
        List<LockCondition> lockConditions,
        Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        return EvaluateLockConditions(lockConditions, interlockingObjects,
            IsEnsuredRouteBySignalControlConditionsPredicate);
    }

    private static bool IsEnsuredRouteBySignalControlConditionsPredicate(
        LockConditionObject o, InterlockingObject interlockingObject)
    {
        // 信号制御欄軌道条件　生軌道が短絡していないかどうか
        return interlockingObject switch
        {
            // 軌道回路が短絡していないこと
            TrackCircuit trackCircuit => !trackCircuit.TrackCircuitState.IsShortCircuit,
            // 転換中でなく、目的方向であること
            SwitchingMachine switchingMachine =>
                !switchingMachine.SwitchingMachineState.IsSwitching
                && switchingMachine.SwitchingMachineState.IsReverse == o.IsReverse,
            // Todo: 進路=>保留 (仮で一旦除外)
            // Todo: てこ=>保留(仮で一旦除外)
            Route or Lever => true,
            _ => false
        };
    }

    /// <summary>
    /// 進路の信号制御欄から、停止を指示する現示を現示する必要があるか確認する
    /// </summary>
    private static bool MustIndicateStopSignalBySignalControlConditions(
        List<LockCondition> lockConditions,
        Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        return !EvaluateLockConditions(lockConditions, interlockingObjects,
            MustIndicateStopSignalPredicate);
    }

    private static bool MustIndicateStopSignalPredicate(
        LockConditionObject o, InterlockingObject interlockingObject)
    {
        return interlockingObject switch
        {
            // 軌道回路が短絡していないこと
            TrackCircuit trackCircuit => !trackCircuit.TrackCircuitState.IsShortCircuit,
            // 進路のてこリレーが落下していること
            Route targetRoute => targetRoute.RouteState.IsLeverRelayRaised == RaiseDrop.Drop,
            // Todo: 転てつ器、てこ => 仮で一旦除外
            SwitchingMachine or Lever => true,
            _ => false
        };
    }

    private static bool MustIndicateStopSignalByDirectLockConditions(
        List<LockCondition> lockConditions,
        Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        return !EvaluateLockConditions(lockConditions, interlockingObjects,
            MustIndicateStopSignalByDirectLockConditionsPredicate);
    }

    private static bool MustIndicateStopSignalByDirectLockConditionsPredicate(LockConditionObject o,
        InterlockingObject interlockingObject)
    {
        return interlockingObject switch
        {
            // 転てつ器が転換中でなく、目的方向であること
            SwitchingMachine switchingMachine =>
                !switchingMachine.SwitchingMachineState.IsSwitching
                && switchingMachine.SwitchingMachineState.IsReverse == o.IsReverse,
            // それ以外はTrueを返す
            _ => true
        };
    }

    /// <summary>
    /// 接近鎖錠欄の条件から、接近区画に列車が接近しているか確認する
    /// </summary>
    private static bool ShouldApproachLock(
        List<LockCondition> lockConditions,
        Dictionary<ulong, InterlockingObject> interlockingObjects)
    {
        return !EvaluateLockConditions(lockConditions, interlockingObjects, ShouldApproachLockPredicate);
    }

    private static bool ShouldApproachLockPredicate(
        LockConditionObject o, InterlockingObject interlockingObject)
    {
        return interlockingObject switch
        {
            // 軌道回路が短絡していないこと
            TrackCircuit trackCircuit => !trackCircuit.TrackCircuitState.IsShortCircuit,
            // 進路の接近鎖錠MRリレーが図表記載方向であること(定位=raise, 反位=drop)
            Route route => (o.IsReverse == NR.Normal &&
                            route.RouteState.IsApproachLockMRRaised == RaiseDrop.Raise)
                           || (o.IsReverse == NR.Reversed &&
                               route.RouteState.IsApproachLockMRRaised == RaiseDrop.Drop),
            // 転てつ器が転換中でなく、目的方向であること
            SwitchingMachine switchingMachine => !switchingMachine.SwitchingMachineState.IsSwitching &&
                                                 switchingMachine.SwitchingMachineState.IsReverse ==
                                                 o.IsReverse,
            DirectionRoute directionRoute => directionRoute.DirectionRouteState.isLr == o.IsLR,
            _ => false
        };
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
            case LockConditionType.Not:
                {
                    var childLockCondition = childLockConditions[lockCondition.Id];
                    if (childLockCondition.Count != 1)
                    {
                        throw new InvalidOperationException("Not条件は1つの条件に対してのみ適用される必要があります。");
                    }

                    return !EvaluateLockCondition(
                        childLockCondition.First(), childLockConditions, interlockingObjects, predicate);
                }
        }

        if (lockCondition is not LockConditionObject lockConditionObject)
        {
            // And, Or以外だとこれしかないので、基本的にはこのルートには入らない想定
            return false;
        }

        return interlockingObjects.TryGetValue(lockConditionObject.ObjectId, out var interlockingObject)
               && predicate(lockConditionObject, interlockingObject);
    }

    private static List<ulong> ExtractObjectIdsFromLockCondtions(
        List<LockCondition> lockConditions)
    {
        return lockConditions.OfType<LockConditionObject>().Select(lc => lc.ObjectId).ToList();
    }
}