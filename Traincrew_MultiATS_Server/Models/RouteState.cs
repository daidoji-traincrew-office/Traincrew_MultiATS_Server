using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("route_state")]
public class RouteState
{
    [Key]
    public ulong Id { get; init; }
    public RaiseDrop IsLeverRelayRaised { get; set; }
    public RaiseDrop IsRouteRelayRaised { get; set; }
    public RaiseDrop IsSignalControlRaised { get; set; }
    public RaiseDrop IsApproachLockMRRaised { get; set; }
    public RaiseDrop IsApproachLockMSRaised { get; set; }
    public RaiseDrop IsRouteLockRaised { get; set; }
    // ReSharper disable InconsistentNaming
    [Column("is_throw_out_xr_relay_raised")]
    public RaiseDrop IsThrowOutXRRelayRaised { get; set; }
    [Column("is_throw_out_ys_relay_raised")]
    public RaiseDrop IsThrowOutYSRelayRaised { get; set; }
    // ReSharper restore InconsistentNaming
}