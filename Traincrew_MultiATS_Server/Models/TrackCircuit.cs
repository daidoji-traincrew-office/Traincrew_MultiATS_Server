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
}