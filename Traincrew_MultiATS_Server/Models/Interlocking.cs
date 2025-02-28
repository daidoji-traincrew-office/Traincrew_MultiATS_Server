using Newtonsoft.Json;
using System.Collections.Generic;

namespace Traincrew_MultiATS_Server.Models
{

    /// <summary>
    /// 常時送信用データクラス
    /// </summary>
    public class ConstantDataFromInterlocking
    {
        /// <summary>
        /// 常時送信用駅データリスト
        /// </summary>
        public List<string> ActiveStationsList { get; set; }
    }

    /// <summary>
    /// イベント送信用データクラス(物理てこ)
    /// </summary>
    public class LeverEventDataFromInterlocking
    {
        /// <summary>
        /// 物理てこデータ
        /// </summary>
        public InterlockingLeverData LeverData { get; set; }
    }

    /// <summary>
    /// イベント送信用データクラス(着点ボタン)
    /// </summary>
    public class ButtonEventDataFromInterlocking
    {
        /// <summary>
        /// 着点ボタンデータ
        /// </summary>
        public DestinationButtonState DestinationButtonData { get; set; }
    }

    /// <summary>
    /// 受信用データクラス
    /// </summary>
    public class DataToInterlocking
    {
        /// <summary>
        /// 認証情報リスト
        /// </summary>
        public TraincrewRole Authentications { get; set; }

        /// <summary>
        /// 軌道回路情報リスト
        /// </summary>
        [JsonProperty("trackCircuitList")]
        public List<TrackCircuitData> TrackCircuits { get; set; }

        /// <summary>
        /// 転てつ器情報リスト
        /// </summary>
        public List<InterlockingSwitchData> Points { get; set; }

        /// <summary>
        /// 信号機情報リスト
        /// </summary>
        [JsonProperty("signalDataList")]
        public List<SignalData> Signals { get; set; }

        /// <summary>
        /// 物理てこ情報リスト
        /// </summary>
        public List<InterlockingLeverData> PhysicalLevers { get; set; }

        /// <summary>
        /// 着点ボタン情報リスト
        /// </summary>
        public List<DestinationButtonState> PhysicalButtons { get; set; }

        /// <summary>
        /// 方向てこ情報リスト
        /// </summary>
        public List<InterlockingDirectionData> Directions { get; set; }

        /// <summary>
        /// 列番情報リスト
        /// </summary>
        public List<InterlockingRetsubanData> Retsubans { get; set; }

        /// <summary>
        /// 表示灯情報リスト
        /// </summary>
        public List<Dictionary<string, bool>> Lamps { get; set; }
    }

    /// <summary>
    /// 方向てこデータクラス
    /// </summary>
    public class InterlockingDirectionData
    {
        /// <summary>
        /// 方向てこ名称
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// 方向てこの値
        /// </summary>
        public LCR State { get; set; } = LCR.Left;
    }

    /// <summary>
    /// 転てつ器データクラス
    /// </summary>
    public class InterlockingSwitchData
    {
        /// <summary>
        /// 転てつ器状態
        /// </summary>
        public NRC State { get; set; } = NRC.Center;
        /// <summary>
        /// 転てつ器名称
        /// </summary>
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// 列番データクラス
    /// </summary>
    public class InterlockingRetsubanData
    {
        /// <summary>
        /// 列番名称
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// 列番
        /// </summary>
        public string Retsuban { get; set; } = "";
    }

    /// <summary>
    /// 物理てこデータクラス
    /// </summary>
    public class InterlockingLeverData
    {
        /// <summary>
        /// 物理てこ名称
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// 物理てこの状態
        /// </summary>
        public LCR State { get; set; } = LCR.Center;
    }
}