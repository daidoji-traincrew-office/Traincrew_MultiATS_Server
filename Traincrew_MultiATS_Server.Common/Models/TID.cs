namespace Traincrew_MultiATS_Server.Common.Models;

/// <summary>
/// 常時送信用データクラス
/// </summary>
public class ConstantDataToTID
{
    /// <summary>
    /// 軌道回路情報リスト
    /// </summary>
    public List<TrackCircuitData> TrackCircuitDatas { get; set; }

    /// <summary>
    /// 転てつ器情報リスト
    /// </summary>
    public List<SwitchData> SwitchDatas { get; set; }

    /// <summary>
    /// 方向てこ情報リスト
    /// </summary>
    public List<DirectionData> DirectionDatas { get; set; }

    /// <summary>
    /// 列車状態情報リスト
    /// </summary>
    public List<TrainStateData> TrainStateDatas { get; set; }
}