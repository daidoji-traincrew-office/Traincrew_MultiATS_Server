// //※DB取得・設定は失敗時速やかに早期リターン
//
// using System.Drawing;
// using Traincrew_MultiATS_Server.Models;
// using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
// using Traincrew_MultiATS_Server.Repositories.LockCondition;
// using Route = Traincrew_MultiATS_Server.Models.Route;
//
// namespace Traincrew_MultiATS_Server.Services;
//
// /// <summary>
// ///     連動装置
// /// </summary>
// public class RendoService(
//     IInterlockingObjectRepository interlockingObjectRepository,
//     ILockConditionRepository lockConditionRepository)
// {
//     /// <summary>
//     ///     指示受付部
//     /// </summary>
//     /// <param name="stationId">駅ID</param>
//     /// <param name="name">てこ名称</param>
//     /// <param name="isR">定位反位</param>
//     public async Task SetTekoState(string stationId, string name, bool isR)
//     {
//         //DB取得:stationIDの連動表情報取得　なかったら早期リターン
//         // Todo: NotFoundの場合の処理を追加
//         var rendoObj = await interlockingObjectRepository.GetObject(stationId, name);
//
//         switch (rendoObj)
//         {
//             //進路てこ
//             case Route route when isR:
//                 await SetRoute(route);
//                 break;
//             case Route route:
//                 ResetRoute(route);
//                 break;
//             case SwitchingMachine:
//                 //転てつてこ
//                 break;
//             default:
//                 //未定義type
//                 Console.WriteLine("SetTekoState:未定義typeのてこを扱った");
//                 return;
//         }
//
//         /*
//         switch (targetRendoTableObjType)
//         {
//             case "DirectionTeko":
//                 //方向てこ
//                 break;
//             case "AutoTeko":
//                 //自動制御てこ・CTC切替てこ
//                 break;
//             case "DirectionOpen":
//                 //開放てこ
//                 break;
//         }
//         */
//     }
//
//     /// <summary>
//     ///     進路反位処理部
//     /// </summary>
//     private async Task SetRoute(Route route)
//     {
//         //DB取得:stationID,nameが総括制御するてこの一覧を取得
//         // Todo: ここワンチャン統合できるかも？
//         //DB設定:stationID,nameが総括制御するてこの内部してほしい状態を反位に設定する
//
//         //DB取得:stationID,nameの信号制御の一覧と該当オブジェクトを取得
//         var rendoExecuteList = await lockConditionRepository.GetConditionsByObjectIdAndType(route.Id, "signal_control");
//
//         var pointMoveList = new List<(ulong, bool)>();
//         foreach (var lockCondition in rendoExecuteList)
//         {
//             switch (lockCondition.Type)
//             {
//                 //rendoExecute.type：鎖錠テーブルの各要素の種類    
//                 //rendoExecute.name：鎖錠テーブルの各要素の種類            
//                 //rendoExecute.id：鎖錠テーブルの各要素のid         
//                 //rendoExecute.teihan：鎖錠テーブルの各要素の目的定反状態
//                 case "object":
//                     switch (lockCondition.targetObject)
//                     {
//                         case TrackCircuit trackCircuit:
//                             //軌道回路要素のとき
//                             //DB取得:rendoExecute.idの軌道回路の鎖錠状態を取得
//                             var trackLock = true;
//                             //DB取得:rendoExecute.idの軌道回路の短絡状態を取得          
//                             var trackOn = true;
//                             if (trackLock != lockCondition.IsReverse || trackOn != lockCondition.IsReverse) return;
//                             break;
//                         case SwitchingMachine switchingMachine:
//                             //転てつ器要素のとき
//                             pointMoveList.Add((switchingMachine.Id, lockCondition.IsReverse));
//                             //後で転換するので、転換すべき転てつ器の情報をまとめておく
//                             break;
//                         default:
//                             //DB取得:rendoExecute.idのてこの定反状態を取得      
//                             var otherTeihan = true;
//                             if (otherTeihan != lockCondition.IsReverse)
//                                 //DB設定:rendoExecute.idに定反転換指令を出す
//                                 return;
//                             break;
//                     }
//
//                     break;
//                 case "timer":
//                     // Todo: どうする？
//                     break;
//             }
//         }
//
//         var AllPoint = false;
//         foreach (var point in pointMoveList)
//         {
//             //DB取得:point.idのてこの定反状態を取得     
//             var pointTeihan = true;
//             if (pointTeihan != point.Item3)
//                 //DB設定:point.id内部指示をpoint.Item3へ変更      
//                 AllPoint = true;
//         }
//
//         //ポイント転換確認
//         if (AllPoint) return;
//
//         foreach (var rendoExecute in rendoExecuteList)
//         {
//             //rendoExecute.type：鎖錠テーブルの各要素の種類    
//             //rendoExecute.name：鎖錠テーブルの各要素の種類            
//             //rendoExecute.id：鎖錠テーブルの各要素のid         
//
//             //DB設定:rendoExecute.idを鎖錠
//         }
//     }
//
//     /// <summary>
//     ///     進路定位処理部
//     /// </summary>
//     private void ResetRoute(Route route)
//     {
//         //DB取得:stationID,nameの反位鎖錠原因リストを取得              
//         if ( /*リストに要素があったら*/)
//             //反位強制
//             return;
//         //DB取得:stationID,nameの進路鎖錠リストを取得    
//         var routeLockList = new List<List<string>>();
//         //DB取得:stationID,nameの進路鎖錠を取得
//         var nowRouteLock = true;
//         if (nowRouteLock)
//         {
//             var OpenTracks = new List<List<string>>();
//             var OpenState = true;
//             foreach (var tracks in routeLockList)
//             {
//                 foreach (var track in tracks)
//                 {
//                     //DB取得:track.idの軌道回路の短絡状態を取得          
//                     var trackOn = true;
//                     if (trackOn)
//                     {
//                         //DB設定:stationID,nameの進路鎖錠を鎖錠に設定   
//                         OpenState = false;
//                         break;
//                     }
//                 }
//
//                 if (OpenState)
//                 {
//                     OpenTracks.Add(tracks);
//                     routeLockList.Remove(tracks);
//                 }
//             }
//
//             foreach (var tracks in OpenTracks)
//             foreach (var track in tracks)
//             {
//                 //DB設定:track.idの軌道回路の鎖錠を解錠に設定      
//             }
//
//             if (routeLockList.Count != 0) return;
//         }
//
//         //進路鎖錠必要かどうか判定部            
//         foreach (var tracks in routeLockList)
//         foreach (var track in tracks)
//         {
//             //DB取得:track.idの軌道回路の短絡状態を取得          
//             var trackOn = true;
//             if (trackOn)
//                 //DB設定:stationID,nameの進路鎖錠を鎖錠に設定   
//                 return;
//         }
//
//         //DB取得:stationID,nameの接近鎖錠を取得
//         var nowApproachLock = true;
//         if (nowApproachLock)
//             //DB設定:stationID,nameの接近鎖錠解錠時刻を取得
//             if ( /*接近鎖錠解錠時刻 < 現時刻*/)
//                 return;
//
//         //接近鎖錠必要かどうか判定部      
//         //DB取得:stationID,nameの接近鎖錠リストを取得    
//         var approachLockList = new List<string>();
//         foreach (var track in approachLockList)
//         {
//             //DB取得:track.idの軌道回路の短絡状態を取得          
//             var trackOn = true;
//             if (trackOn)
//                 //DB設定:stationID,nameの接近鎖錠を鎖錠に設定   
//                 //DB取得:stationID,nameの接近鎖錠時素秒数を取得        
//                 //DB設定:stationID,nameの接近鎖錠解錠時刻を現在時刻+時素秒数に設定
//                 return;
//         }
//         //鎖錠原因全通過
//         //DB設定:stationID,nameの内部状態を定位に設定   
//     }
//
//     /// <summary>
//     ///     転てつ器処理部
//     /// </summary>
//     private void PointSet(string stationID, string name, bool isR)
//     {
//         //DB取得:stationID,nameの転換状態を取得  
//         var State = true;
//         if (isR == State)
//         {
//             //同じだから何もしない
//         }
//         else if (isR == 転換中)
//         {
//             //DB取得:stationID,nameの転換終了時刻を取得    
//             if ( /*接近鎖錠解錠時刻 < 現時刻*/)
//             {
//             }
//         }
//         else
//         {
//             //DB取得:stationID,nameのてっ査鎖錠リストを取得                  
//             //DB設定:stationID,nameの接近鎖錠解錠時刻を現在時刻+時素秒数に設定
//             var detectorLockList = new List<string>();
//             foreach (var track in detectorLockList)
//             {
//                 //DB取得:rendoExecute.idの軌道回路の鎖錠状態を取得
//                 var trackLock = true;
//                 //DB取得:rendoExecute.idの軌道回路の短絡状態を取得          
//                 var trackOn = true;
//                 if (trackLock || trackOn)
//                     //てっ査鎖錠されている
//                     return;
//             }
//             //DB設定:stationID,nameの転換状態を転換中に設定         
//             //DB設定:stationID,nameの転換状態を転換終了時刻を現在時刻+転換必要時間に設定  
//         }
//     }
// }