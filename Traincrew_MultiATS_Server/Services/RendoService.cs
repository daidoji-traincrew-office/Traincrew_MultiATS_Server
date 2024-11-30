using Microsoft.IdentityModel.Tokens;
using System.Collections.Immutable;

namespace Traincrew_MultiATS_Server.Services
{
    /// <summary>
    /// 連動装置
    /// </summary>
    public class RendoService
    {
        /// <summary>
        /// 指示受付部
        /// </summary>                         
        /// <param name="stationID">駅ID</param>
        /// <param name="name">てこ名称</param>
        /// <param name="isR">定位反位</param>
        public void SetTekoState(string stationID, string name, bool isR)
        {
            //DB取得:stationIDの連動表情報取得　なかったら早期リターン
            var rendoObj = new Object();
            //DB取得:Nameの情報取得　なかったら早期リターン    
            var targetRendoTableObj = new Object();
            //DB取得:Nameのtype取得        
            var targetRendoTableObjType = "";

            switch (targetRendoTableObjType)
            {
                case "DirectionTeko":
                    //方向てこ
                    break;
                case "Point":
                    //転轍てこ
                    break;
                case "ArrSignal":
                case "DepSignal":
                case "SwitchSign":
                case "SwitchSignal":
                case "GuideSignal":
                    //進路てこ
                    if (isR)
                    {

                    }
                    else
                    {

                    }
                    break;
                case "AutoTeko":
                    //自動制御てこ・CTC切替てこ
                    break;
                case "DirectionOpen":
                    //開放てこ
                    break;
                default:
                    //未定義type
                    Console.WriteLine("SetTekoState:未定義typeのてこを扱った");
                    return;
            }
        }

        /// <summary>
        /// 進路反位処理部
        /// </summary>
        private void SetRoute(string stationID, string name, bool isR)
        {
            //DB取得:stationID,nameが総括制御するてこの一覧を取得

            //DB設定:stationID,nameが総括制御するてこの内部してほしい状態を反位に設定する

            //DB取得:stationID,nameの信号制御の一覧を取得
            var rendoExecuteList = new List<string>();

            var pointMoveList = new List<Tuple<string, string, bool>>();
            foreach (var rendoExecute in rendoExecuteList)
            {
                //rendoExecute.type：鎖錠テーブルの各要素の種類    
                //rendoExecute.name：鎖錠テーブルの各要素の種類            
                //rendoExecute.id：鎖錠テーブルの各要素のid         
                //rendoExecute.teihan：鎖錠テーブルの各要素の目的定反状態
                if (rendoExecute.type == "Track")
                {
                    //軌道回路要素のとき
                    //DB取得:rendoExecute.idの軌道回路の鎖錠状態を取得
                    var trackLock = true;
                    //DB取得:rendoExecute.idの軌道回路の短絡状態を取得          
                    var trackOn = true;
                    if (trackLock != rendoExecute.teihan || trackOn != rendoExecute.teihan)
                    {
                        return;
                    }
                }
                else if (rendoExecute.type == "Point")
                {
                    //転てつ器要素のとき
                    pointMoveList.Add(rendoExecute.type, rendoExecute.name, rendoExecute.teihan);
                    //後で転換するので、転換すべき転てつ器の情報をまとめておく
                }
                else
                {
                    //DB取得:rendoExecute.idのてこの定反状態を取得      
                    var otherTeihan = true;
                    if (otherTeihan != rendoExecute.teihan)
                    {
                        return;
                    }
                }
            }
            //DB設定:pointMoveListの各要素の情報に従い内部指示を目的方向へ変更
            foreach (var point in pointMoveList)
            {
                //DB取得:point.Item1(駅名), point.Item2(番号)の転換状態を取得
                var nowState = 1;
                if (point.Item3 != nowState)
                {
                    return;
                }
            }
            //DB設定:stationID,nameの内部状態を反位にする
        }

        /// <summary>
        /// 進路定位処理部
        /// </summary>
        private void ResetRoute(string stationID, string name, bool isR)
        {

        }
    }
}
