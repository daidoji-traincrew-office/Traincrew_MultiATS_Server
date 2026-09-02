using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

/// <summary>
/// 注意: このテーブルは2プロセスが別々の列を書く。全カラム上書きは禁止。
/// </summary>
[Table("track_circuit_state")]
public class TrackCircuitState
{
    public ulong Id { get; init; }
    public required string TrainNumber { get; set; }
    /// <summary>
    /// 親TR相当
    /// RaiseDropではない
    /// </summary>
    public bool IsShortCircuit { get; set; }
    public bool IsLocked { get; set; }
    public ulong? LockedBy { get; set; }
    [Column(TypeName = "timestamp without time zone")]
    public DateTime? UnlockedAt { get; set; }
}
