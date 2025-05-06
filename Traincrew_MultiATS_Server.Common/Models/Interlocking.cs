namespace Traincrew_MultiATS_Server.Common.Models
{
    /// <summary>
    /// 受信用データクラス
    /// </summary>
    public class DataToInterlocking
    {
        /// <summary>
        /// 軌道回路情報リスト
        /// </summary>
        public List<TrackCircuitData> TrackCircuits { get; set; }

        /// <summary>
        /// 転てつ器情報リスト
        /// </summary>
        public List<SwitchData> Points { get; set; }

        /// <summary>
        /// 信号機情報リスト
        /// </summary>
        public List<SignalData> Signals { get; set; }

        /// <summary>
        /// 物理てこ情報リスト
        /// </summary>
        public List<InterlockingLeverData> PhysicalLevers { get; set; }

        /// <summary>
        /// 物理鍵てこ情報リスト
        /// </summary>
        public List<InterlockingKeyLeverData> PhysicalKeyLevers { get; set; }

        /// <summary>
        /// 着点ボタン情報リスト
        /// </summary>
        public List<DestinationButtonData> PhysicalButtons { get; set; }

        /// <summary>
        /// 方向てこ情報リスト
        /// </summary>
        public List<DirectionData> Directions { get; set; }

        /// <summary>
        /// 列番情報リスト
        /// </summary>
        public List<InterlockingRetsubanData> Retsubans { get; set; }

        /// <summary>
        /// 表示灯情報リスト
        /// </summary>
        public Dictionary<string, bool> Lamps { get; set; }
    }

    /// <summary>
    /// 転てつ器データクラス
    /// </summary>
    public class SwitchData
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
    /// 方向てこデータクラス
    /// </summary>
    public class DirectionData
    {
        /// <summary>
        /// 方向てこ名称
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// 方向てこの値
        /// </summary>
        public LCR State { get; set; } = LCR.Center;
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

    /// <summary>
    /// 物理鍵てこデータクラス
    /// </summary>
    public class InterlockingKeyLeverData
    {
        /// <summary>
        /// 物理鍵てこ名称
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// 物理鍵てこの状態
        /// </summary>
        public LNR State { get; set; } = LNR.Normal;
        /// <summary>
        /// 物理鍵てこの鍵挿入状態
        /// </summary>
        public bool IsKeyInserted { get; set; } = false;
    }
}