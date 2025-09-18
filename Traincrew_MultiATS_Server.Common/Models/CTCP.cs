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

    /// <summary>
    /// 信号機情報リスト
    /// </summary>
    public List<SignalData> Signals { get; set; }

    /// <summama
    /// CTCてこ情報リスト
    /// </summary>
    public List<InterlockingLeverData> CTCLevers { get; set; }

    /// <summary>
    /// 物理鍵てこ情報リスト
    /// </summary>
    public List<InterlockingKeyLeverData> PhysicalKeyLevers { get; set; }

    /// <summary>
    /// 列番情報リスト
    /// </summary>
    public List<InterlockingRetsubanData> Retsubans { get; set; }

    /// <summary>
    /// 表示灯情報リスト
    /// </summary>
    public Dictionary<string, bool> Lamps { get; set; }
}