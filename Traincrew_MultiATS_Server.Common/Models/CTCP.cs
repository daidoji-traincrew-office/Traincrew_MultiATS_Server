namespace Traincrew_MultiATS_Server.Common.Models;

/// <summary>
/// CTCP送信用データクラス
/// </summary>
public class DataToCTCP
{
    /// <summary>
    /// 軌道回路情報リスト
    /// </summary>
    public List<TrackCircuitData> TrackCircuits { get; set; }

    /// <summama
    /// CTCてこ情報リスト
    /// </summary>
    public List<RouteData> RouteDatas { get; set; }

    /// <summary>
    /// 集中・駅扱状態
    /// </summary>
    public Dictionary<string, CenterControlState> CenterControlStates { get; set; }

    /// <summary>
    /// 列番情報リスト
    /// </summary>
    public List<InterlockingRetsubanData> Retsubans { get; set; }

    /// <summary>
    /// 表示灯情報リスト
    /// </summary>
    public Dictionary<string, bool> Lamps { get; set; }
}

public enum CenterControlState
{
    StationControl,
    CenterControl
}