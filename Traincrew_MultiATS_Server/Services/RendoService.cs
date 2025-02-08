//※DB取得・設定は失敗時速やかに早期リターン

using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Services;

/// <summary>
///     連動装置
/// </summary>
public class RendoService(
    IInterlockingObjectRepository interlockingObjectRepository,
    ILockConditionRepository lockConditionRepository)
{
    /// <summary>
    /// <strong>てこリレー回路</strong><br/>
    /// てこやボタンの状態から、確保するべき進路を決定する。
    /// </summary>
    /// <returns></returns>
    public async Task LevertoRouteState()
    {
        // Todo: RouteLeverDestinationButtonを全取得
        var RouteLeverDestinationButtonList = new List<RouteLeverDestinationButton>();

        foreach (var RouteLeverDestinationButton in RouteLeverDestinationButtonList)
        {
            // Todo: 鎖錠確認

            // Todo: CTC制御状態を確認する(CHR相当)

            // Todo:  RouteLeverDestinationButton.RouteIdの進路のRouteState.IsLeverRelayRaisedを取得
            var isLeverRelayRaised = false;

            // Todo: RouteLeverDestinationButton.LeverNameのレバー状態を取得
            var leverState = LCR.Center;

            if (leverState == LCR.Center)
            {
                if (isLeverRelayRaised)
                {
                    isLeverRelayRaised = false;
                }
                continue;
            }
            else if (leverState == LCR.Left || leverState == LCR.Right)
            {
                if (isLeverRelayRaised)
                {
                    continue;
                }
                // Todo: RouteLeverDestinationButton.DestinationButtonNameのDestinationButtonState.IsRaisedを取得
                var isRaised = false;
                if (isRaised)
                {
                    isLeverRelayRaised = true;
                }
            }
            // Todo: RouteLeverDestinationButton.RouteIdの進路のRouteState.IsLeverRelayRaisedに変化あればisLeverRelayRaised代入

            //ここから仮コード　本番に残さない

            // Todo: RouteLeverDestinationButton.RouteIdの進路のRouteState.IsSignalControlRaisedに変化あればisLeverRelayRaised代入

            //仮コードここまで
        }
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