using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Models;

[Table("station_timer_state")]
public class StationTimerState
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; init; }
    public required string StationId { get; set; }
    public required int Seconds { get; init; }
    public RaiseDrop IsTeuRelayRaised { get; set; }
    public RaiseDrop IsTenRelayRaised { get; set; }
    public RaiseDrop IsTerRelayRaised { get; set; }
    [Column(TypeName = "timestamp without time zone")]
    public DateTime? TeuRelayRaisedAt { get; set; }
}