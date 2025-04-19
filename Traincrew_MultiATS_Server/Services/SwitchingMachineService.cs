using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Services;

public class SwitchingMachineService(
    IDateTimeRepository dateTimeRepository,
    ISwitchingMachineRepository switchingMachineRepository,
    IInterlockingObjectRepository interlockingObjectRepository,
    ISwitchingMachineRouteRepository switchingMachineRouteRepository,
    ILockConditionRepository lockConditionRepository,
    ILeverRepository leverRepository,
    IGeneralRepository generalRepository
    )
{
    /// <summary>
    /// <strong>転てつ器制御回路・転てつ器表示リレー回路</strong><br/>
    /// 現在の状態から、転てつ器の状態を決定する。表示リレー回路直接は存在せず、IsSwitchingとIsReverseの組み合わせを毎度取得する。
    /// </summary>
    /// <returns></returns>   
    public async Task SwitchingMachineControl()
    {
        // こいつは定常で全駅回すので駅ごとに分けるやつの対象外
        // Todo: クラスのstaticにしたほうが良いかも
        var switchMoveTime = TimeSpan.FromSeconds(5);
        var switchReturnTime = TimeSpan.FromMilliseconds(500);
        // 処理が必要な転てつ器のIDを取得
        // 1. 転換中の転てつ器
        var movingSwitchingMachineIds = await switchingMachineRepository.GetIdsWhereMoving();
        // 2. 転てつ器の単独てこが倒れている転てつ器
        var leverReversedSwitchingMachineIds = await switchingMachineRepository.GetIdsWhereLeverReversed();
        // 3. てこリレー回路が上がっている進路に対する転てつ器
        var leverRelayRaisedSwitchingMachineIds = await switchingMachineRepository.GetIdsWhereLeverRelayRaised();
        // それら全てのID
        var switchingMachineIds = movingSwitchingMachineIds
            .Union(leverReversedSwitchingMachineIds)
            .Union(leverRelayRaisedSwitchingMachineIds)
            .ToList();
        
        // 転てつ器てこのIDを取得
        var leverIds = await leverRepository.GetIdsBySwitchingMachineIds(switchingMachineIds); 
        
        // 進路のIDを取得
        var switchingMachineRoutes =
            await switchingMachineRouteRepository.GetBySwitchingMachineIds(switchingMachineIds);
        var routeIds = switchingMachineRoutes.Select(route => route.RouteId).ToList();

        // てっさ鎖錠欄の条件を取得
        var detectorLockConditions = await lockConditionRepository.GetConditionsByObjectIdsAndType(routeIds, LockType.Detector);

        // 関係するObjectのIDを取得
        var detectorLockObjects = detectorLockConditions.Values
            .SelectMany(lockConditions => lockConditions.OfType<LockConditionObject>().Select(lc => lc.ObjectId))
            .Distinct()
            .ToList();

        // 全IDをUnionしてInterlockingObjectを取得
        var allObjectIds = switchingMachineIds
            .Union(leverIds)
            .Union(routeIds)
            .Union(detectorLockObjects)
            .ToList();
        var interlockingObjects = await interlockingObjectRepository.GetObjectByIdsWithState(allObjectIds);
        var interlockingObjectDic = interlockingObjects.ToDictionary(x => x.Id);
        // 転てつ機
        var switchingMachineList = interlockingObjects
            .OfType<SwitchingMachine>()
            .ToList();
        // 転てつ機てこ
        var levers = interlockingObjects
            .OfType<Lever>()
            .Where(x => x.SwitchingMachineId != null)
            .ToDictionary(x => x.SwitchingMachineId.Value);
        // SwitchingMachineRouteのDict
        var switchingMachineRouteDict = switchingMachineRoutes 
            .GroupBy(x => x.SwitchingMachineId)
            .ToDictionary(x => x.Key, x => x.ToList());
        // 進路のDict
        var routes = interlockingObjects
            .OfType<Route>()
            .ToDictionary(x => x.Id);

        foreach (var switchingMachine in switchingMachineList)
        {
            // 対応する転てつ器のてっさ鎖錠欄の条件確認
            var detectorLockCondition = detectorLockConditions.GetValueOrDefault(switchingMachine.Id, []);

            if (RendoService.IsDetectorLocked(detectorLockCondition, interlockingObjectDic))
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
            var isRoutelock = false;

            // 対応する転てつ器の要求進路一覧を取得
            // Todo: 全部データをちゃんと入れたら、デフォルト値を使わないようにする
            var switchingMachineRouteList = switchingMachineRouteDict.GetValueOrDefault(switchingMachine.Id, []);

            foreach (var switchingMachineRoute in switchingMachineRouteList)
            {
                // 対応する進路のRouteState.IsLeverRelayRaisedを取得
                var route = routes[switchingMachineRoute.RouteId];

                // 進路鎖錠欄に直接てっさ鎖錠軌道回路がない場合(=過走防護関係の転てつ器の場合)、進路鎖錠を受けていたら強制終了
                if (!switchingMachineRoute.OnRouteLock && route.RouteState.IsRouteLockRaised == RaiseDrop.Drop)
                {
                    isRoutelock = true;
                    break;
                }
                // 進路鎖錠欄に直接てっさ鎖錠軌道回路がある場合、IsDetectorLockedですでに弾かれている

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
            if (isRoutelock)
            {
                //鎖錠中
                continue;
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

    public static SwitchData ToSwitchData(SwitchingMachine switchingMachine)
    {
        var state = switchingMachine.SwitchingMachineState;

        return new()
        {
            Name = switchingMachine.Name,
            State = state.IsSwitching ? NRC.Center : state.IsReverse == NR.Normal ? NRC.Normal : NRC.Reversed
        };
    }

    public async Task<List<SwitchData>> GetAllSwitchData()
    {
        var switchingMachines = await switchingMachineRepository.GetSwitchingMachinesWithState();
        return switchingMachines.Select(ToSwitchData).ToList();
    }
}
