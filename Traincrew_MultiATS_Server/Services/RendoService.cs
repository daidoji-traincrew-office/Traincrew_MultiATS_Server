//※DB取得・設定は失敗時速やかに早期リターン

using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Button;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.RouteLeverDestinationButton;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
///     連動装置
/// </summary>
public class RendoService(
    IRouteLeverDestinationRepository routeLeverDestinationRepository,
    IInterlockingObjectRepository interlockingObjectRepository,
    IButtonRepository buttonRepository,
    GeneralRepository generalRepository)
{
    /// <summary>
    /// <strong>てこリレー回路</strong><br/>
    /// てこやボタンの状態から、確保するべき進路を決定する。
    /// </summary>
    /// <returns></returns>
    private async Task LeverToRouteState()
    {
        // Todo: あまりにもメモリを爆食いするようなら、以下のように変更する？
        //       駅ごとに処理するように
        //       N+1クエリを許容するように
        // Hope: クエリ自体が重すぎて時間計算量的に死ぬってことはないと信じたい
        // RouteLeverDestinationButtonを全取得
        var routeLeverDestinationButtonList = await routeLeverDestinationRepository.GetAll();
        // InterlockingObjectを全取得
        var interlockingObjects = await interlockingObjectRepository.GetAllWithState();
        // Buttonを全取得
        var buttons = await buttonRepository.GetAllButtons();
        // Todo: 直接鎖状条件を取得
        var lockConditions = new Dictionary<ulong, List<LockCondition>>();
        
        // ここまで実行できればほぼほぼOOMしないはず
        foreach (var routeLeverDestinationButton in routeLeverDestinationButtonList)
        {
            // 進路でない場合 or 進路オブジェクトを取得できない場合はスキップ
            if (!interlockingObjects.TryGetValue(routeLeverDestinationButton.RouteId, out var routeObject) 
                || routeObject is not Route route
                || route.RouteState == null)
            {
                continue;
            }
            var routeState = route.RouteState;
            // てこでない場合 or てこオブジェクトを取得できない場合はスキップ
            if(!interlockingObjects.TryGetValue(routeLeverDestinationButton.LeverId, out var leverObject)
               || leverObject is not Lever lever
               || lever.LeverState == null)
            {
                continue;
            }
            // ボタンオブジェクトを取得できない場合はスキップ
            if (!buttons.TryGetValue(routeLeverDestinationButton.DestinationButtonName, out var button))
            {
                continue;
            }
            // 鎖錠確認 進路の鎖錠欄の条件を満たしていない場合早期continue
            if (!IsLocked(lockConditions[routeLeverDestinationButton.RouteId], interlockingObjects))
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
                // Question: ボタン状態は？
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
                if (isLeverRelayRaised == RaiseDrop.Raise)
                {
                    continue;
                }
                var isRaised = button.ButtonState.IsRaised;
                if (isRaised == RaiseDrop.Raise)
                {
                    routeState.IsLeverRelayRaised = RaiseDrop.Raise;
                    await generalRepository.Save(routeState);
                }
            }
            // Todo: RouteLeverDestinationButton.RouteIdの進路のRouteState.IsLeverRelayRaisedに変化あればisLeverRelayRaised代入

            //ここから仮コード　本番に残さない

            //第二段階用
            // Todo: [繋ぎ込み]RouteLeverDestinationButton.RouteIdの進路のRouteState.IsSignalControlRaisedに変化あればIsRouteRelayRaised代入

            //第一段階用
            // Todo: [繋ぎ込み]RouteLeverDestinationButton.RouteIdの進路のRouteState.IsSignalControlRaisedに変化あればisLeverRelayRaised代入

            //仮コードここまで
        }
    }

    /// <summary>
    /// <strong>進路照査リレー回路</strong><br/>
    /// 現在の状態から、進路が確保されているか決定する。
    /// </summary>
    /// <returns></returns>   
    public async Task RouteRelay()
    {
        // Todo: てこリレーが扛上している進路を全て取得
        // Todo: [繋ぎ込み]RouteState.IsLeverRelayRaisedがRaisedな進路を取得   
        List<Route> routes = [];
        foreach (var route in routes)
        {
            // Todo: [繋ぎ込み]進路のRouteState.IsLeverRelayRaisedを取得   
            var isLeverRelayRaised = false;
            if (!isLeverRelayRaised)
            {
                continue;
            }

            // Todo: [繋ぎ込み]進路の鎖錠欄の条件を満たしているかを取得(転轍器では、目的方向で鎖錠・進路ではその進路のIsRouteRelayRaisedがFalse)   
            var lockState = true;
            if (!lockState)
            {
                continue;
            }

            // Todo: [繋ぎ込み]進路の信号制御欄の条件を満たしているか確認  
            var signalControlState = true;
            if (!signalControlState)
            {
                continue;
            }


            // Todo: 鎖錠確認 進路の鎖錠欄の条件を満たしていない場合早期continue
        }
    }

    /// <summary>
    /// <strong>信号制御リレー回路</strong><br/>
    /// 現在の状態から、進行を指示する信号を現示してよいか決定する。
    /// </summary>
    /// <returns></returns>
    public async Task SignalControl()
    {
        // Todo: 進路照査リレーが扛上している進路及び信号機を取得
        // Todo: [繋ぎ込み]進路のRouteState.IsRouteRelayRaisedを取得   
        List<Route> routes = [];
        foreach (var route in routes)
        {
            // Todo: [繋ぎ込み]進路のRouteState.IsRouteRelayRaisedを取得   
            var isRouteRelayRaised = false;
            if (!isRouteRelayRaised)
            {
                continue;
            }

            // Todo: [繋ぎ込み]進路の鎖錠欄のうち転轍器のてっさ鎖錠がかかっているか

            // Todo: 鎖錠確認 進路の鎖錠欄の条件を満たしていない場合早期continue
        }
    }
    
    /// <summary>
    /// <strong>鎖錠確認</strong><br/>
    /// 鎖状の条件を確認し、鎖状されていればtrueを返す
    /// </summary>
    /// <returns></returns>
    private bool IsLocked(List<LockCondition> lockConditions, Dictionary<ulong, InterlockingObject> interlockingObjects)
    {

        return lockConditions.All(conditions =>
        {
            // 直接鎖状にAndやOrは使えない想定
            // Todo: ホンマか？
            if (conditions is not LockConditionObject lockConditionObject)
            {
                return true;
            }

            // Objectが取れなかった場合もとりあえず鎖状されているとみなす
            if (!interlockingObjects.TryGetValue(lockConditionObject.ObjectId, out var interlockingObject))
            {
                return true;
            }

            return interlockingObject switch
            {
                TrackCircuit trackCircuit => trackCircuit.TrackCircuitState.IsLocked,
                // Todo: 他のオブジェクトの鎖状状態を取得する処理を追加
                SwitchingMachine switchingMachine => true,
                Route route => true,
                _ => true
            };
        });
    }
        
    
    /// <summary>
    ///     進路反位処理部
    /// </summary>
    private async Task SetRoute(Route route)
    {
        // //DB取得:stationID,nameが総括制御するてこの一覧を取得
        // // Todo: ここワンチャン統合できるかも？
        // // Todo: これ鎖状タイプ何？
        // //DB設定:stationID,nameが総括制御するてこの内部してほしい状態を反位に設定する
        //
        // //stationID,nameの信号制御の一覧を取得
        // var rendoExecuteList = await lockConditionRepository
        //     .GetConditionsByObjectIdAndType(route.Id, LockType.SignalControl);
        // // 該当オブジェクトを取得
        // var objectIds = rendoExecuteList
        //     .Where(x => x.ObjectId.HasValue)
        //     .Select(x => x.ObjectId.Value);
        // var objectList = await interlockingObjectRepository
        //     .GetObjectByIdsWithState(objectIds);
        // var objectMap = objectList.ToDictionary(x => x.Id);
        // // Todo: 各オブジェクトの鎖状状態を取得する処理を追加
        //
        // var switchingMachineMoveList = new List<(ulong, NR)>();
        // foreach (var lockCondition in rendoExecuteList)
        // {
        //     switch (lockCondition.Type)
        //     {
        //         //lockCondition.type：鎖錠テーブルの各要素の種類    
        //         //lockCondtion.teihan：鎖錠テーブルの各要素の目的定反状態
        //         case "object":
        //             Debug.Assert(lockCondition.ObjectId != null, "lockCondition.type is object but lockCondition.ObjectId is null");
        //             // 該当オブジェクト
        //             var targetObject = objectMap[lockCondition.ObjectId.Value];
        //             switch (targetObject)
        //             {
        //                 case TrackCircuit trackCircuit:
        //                     //軌道回路要素のとき
        //                     //Todo: DB取得:rendoExecute.idの軌道回路の鎖錠状態を取得
        //                     var trackLock = NR.Normal;
        //                     //DB取得:rendoExecute.idの軌道回路の短絡状態を取得          
        //                     var trackOn = trackCircuit.TrackCircuitState.IsShortCircuit;
        //                     if (trackLock != lockCondition.IsReverse || trackOn) return;
        //                     break;
        //                 case SwitchingMachine switchingMachine:
        //                     //転てつ器要素のとき
        //                     //後で転換するので、転換すべき転てつ器の情報をまとめておく
        //                     switchingMachineMoveList.Add((switchingMachine.Id, lockCondition.IsReverse));
        //                     break;
        //                 default:
        //                     // Routeの場合？
        //                     //Todo: DB取得:rendoExecute.idのてこの定反状態を取得      
        //                     var otherTeihan = NR.Normal;
        //                     if (otherTeihan != lockCondition.IsReverse)
        //                         //Todo: DB設定:rendoExecute.idに定反転換指令を出す
        //                         return;
        //                     break;
        //             }
        //
        //             break;
        //         case "timer":
        //             // Todo: どうする？
        //             break;
        //     }
        // }
        //
        // var AllPoint = false;
        // foreach (var (id, isReverse) in switchingMachineMoveList)
        // {
        //     // Todo: 転轍機処理をSwitchingMachineServiceに移動
        //     /*
        //     var switchingMachine = objectMap[id] as SwitchingMachine;
        //     Debug.Assert(switchingMachine != null, "object is not SwitchingMachine"); 
        //     //DB取得:point.idのてこの定反状態を取得     
        //     var pointTeihan = switchingMachine.SwitchingMachineState.IsReverse;
        //     if (pointTeihan != point.Item3)
        //     {
        //         //DB設定:point.id内部指示をpoint.Item3へ変更      
        //         AllPoint = true;
        //     }
        //     */ 
        // }
        //
        // //ポイント転換確認
        // if (AllPoint) return;
        //
        // // Todo: 鎖状状態リストを作成
        // foreach (var rendoExecute in rendoExecuteList)
        // {
        //     //rendoExecute.type：鎖錠テーブルの各要素の種類    
        //     //rendoExecute.name：鎖錠テーブルの各要素の種類            
        //     //rendoExecute.id：鎖錠テーブルの各要素のid         
        //
        //     //DB設定:rendoExecute.idを鎖錠
        // }
        // // Todo: 最後にDB設定鎖状状態リストをInsert
    }

    /// <summary>
    ///     進路定位処理部
    /// </summary>
    private async Task ResetRoute(Route route)
    {
        // //DB取得:stationID,nameの反位鎖錠原因リストを取得              
        // if ( /*リストに要素があったら*/)
        //     //反位強制
        //     return;
        // //DB取得:stationID,nameの進路鎖錠リストを取得    
        // var routeLockList = new List<List<string>>();
        // //DB取得:stationID,nameの進路鎖錠を取得
        // var nowRouteLock = true;
        // if (nowRouteLock)
        // {
        //     var OpenTracks = new List<List<string>>();
        //     var OpenState = true;
        //     foreach (var tracks in routeLockList)
        //     {
        //         foreach (var track in tracks)
        //         {
        //             //DB取得:track.idの軌道回路の短絡状態を取得          
        //             var trackOn = true;
        //             if (trackOn)
        //             {
        //                 //DB設定:stationID,nameの進路鎖錠を鎖錠に設定   
        //                 OpenState = false;
        //                 break;
        //             }
        //         }
        //
        //         if (OpenState)
        //         {
        //             OpenTracks.Add(tracks);
        //             routeLockList.Remove(tracks);
        //         }
        //     }
        //
        //     foreach (var tracks in OpenTracks)
        //     foreach (var track in tracks)
        //     {
        //         //DB設定:track.idの軌道回路の鎖錠を解錠に設定      
        //     }
        //
        //     if (routeLockList.Count != 0) return;
        // }
        //
        // //進路鎖錠必要かどうか判定部            
        // foreach (var tracks in routeLockList)
        // foreach (var track in tracks)
        // {
        //     //DB取得:track.idの軌道回路の短絡状態を取得          
        //     var trackOn = true;
        //     if (trackOn)
        //         //DB設定:stationID,nameの進路鎖錠を鎖錠に設定   
        //         return;
        // }
        //
        // //DB取得:stationID,nameの接近鎖錠を取得
        // var nowApproachLock = true;
        // if (nowApproachLock)
        //     //DB設定:stationID,nameの接近鎖錠解錠時刻を取得
        //     if ( /*接近鎖錠解錠時刻 < 現時刻*/)
        //         return;
        //
        // //接近鎖錠必要かどうか判定部      
        // //DB取得:stationID,nameの接近鎖錠リストを取得    
        // var approachLockList = new List<string>();
        // foreach (var track in approachLockList)
        // {
        //     //DB取得:track.idの軌道回路の短絡状態を取得          
        //     var trackOn = true;
        //     if (trackOn)
        //         //DB設定:stationID,nameの接近鎖錠を鎖錠に設定   
        //         //DB取得:stationID,nameの接近鎖錠時素秒数を取得        
        //         //DB設定:stationID,nameの接近鎖錠解錠時刻を現在時刻+時素秒数に設定
        //         return;
        // }
        // //鎖錠原因全通過
        // //DB設定:stationID,nameの内部状態を定位に設定   
    }
    /// <summary>
    ///     転てつ器処理部
    /// </summary>
    private async Task PointSet(SwitchingMachine switchingMachine, bool isReverse)
    {
        // //DB取得:stationID,nameの転換状態を取得  
        // var State = true;
        // if (isR == State)
        // {
        //     //同じだから何もしない
        // }
        // else if (isR == 転換中)
        // {
        //     //DB取得:stationID,nameの転換終了時刻を取得    
        //     if ( /*接近鎖錠解錠時刻 < 現時刻*/)
        //     {
        //     }
        // }
        // else
        // {
        //     //DB取得:stationID,nameのてっ査鎖錠リストを取得                  
        //     //DB設定:stationID,nameの接近鎖錠解錠時刻を現在時刻+時素秒数に設定
        //     var detectorLockList = new List<string>();
        //     foreach (var track in detectorLockList)
        //     {
        //         //DB取得:rendoExecute.idの軌道回路の鎖錠状態を取得
        //         var trackLock = true;
        //         //DB取得:rendoExecute.idの軌道回路の短絡状態を取得          
        //         var trackOn = true;
        //         if (trackLock || trackOn)
        //             //てっ査鎖錠されている
        //             return;
        //     }
        //     //DB設定:stationID,nameの転換状態を転換中に設定         
        //     //DB設定:stationID,nameの転換状態を転換終了時刻を現在時刻+転換必要時間に設定  
        // }
    }


}