using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Common.Models;

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
    public ulong? LockedBy { get; set; }
    [Column(TypeName = "timestamp without time zone")]
    public DateTime? UnlockedAt { get; set; }
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