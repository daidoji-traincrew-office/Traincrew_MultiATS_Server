using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("track_circuit")]
public class TrackCircuit : InterlockingObject
{
    public int ProtectionZone { get; set; }

    public virtual TrackCircuitState TrackCircuitState { get; set; }

    [Column("operation_notification_display_name")]
    public string? OperationNotificationDisplayName { get; set; } // 告知器の名前 (Nullable)

    public virtual OperationNotificationDisplay? OperationNotificationDisplay { get; set; } // 告知器との関連

    [Column("station_id_for_delay")]
    public string? StationIdForDelay { get; set; }

    // ナビゲーションプロパティ
    [ForeignKey(nameof(StationIdForDelay))]
    public virtual Station? StationForDelay { get; set; }

    internal override TrackCircuit ShallowClone() => (TrackCircuit)MemberwiseClone();

    /// <summary>
    /// マスタの複製に状態を載せたインスタンスを返す。
    /// </summary>
    /// <remarks>
    /// マスタのスナップショットはプロセス全体で共有されるため、共有インスタンスに
    /// <see cref="TrackCircuitState"/> を直接代入するとリクエスト間でデータ競合する。
    /// 状態を載せるときは必ずこのメソッドを通し、複製に対して載せること。
    /// なお、ここで得た複製を IGeneralRepository.Save に渡してはならない
    /// (InterlockingObjectMaster の注意書きを参照)。
    /// </remarks>
    internal TrackCircuit CloneWithState(TrackCircuitState state)
    {
        var clone = ShallowClone();
        clone.TrackCircuitState = state;
        return clone;
    }
}
