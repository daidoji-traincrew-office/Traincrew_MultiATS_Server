using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

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
}