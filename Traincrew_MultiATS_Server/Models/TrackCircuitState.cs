using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

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
    [Column(TypeName = "timestamp without time zone")]
    public DateTime? AnLockedAt { get; set; }
    public virtual TrackCircuit TrackCircuit { get; set; }
    /// <summary>
    /// 不正扛上補正済みリレー
    /// F付リレー
    /// </summary>
    public RaiseDrop IsCorrectionRaiseRelayRaised { get; set; }
    [Column(TypeName = "timestamp without time zone")]
    public DateTime? RaisedAt { get; set; }
    /// <summary>
    /// 不正落下補正済みリレー
    /// B付リレー
    /// </summary>
    public RaiseDrop IsCorrectionDropRelayRaised { get; set; }
    [Column(TypeName = "timestamp without time zone")]
    public DateTime? DroppedAt { get; set; }
}