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
    public RaiseDrop IsApproachLockRaised { get; set; }
    public RaiseDrop IsRouteLockRaised { get; set; }
    public Route? Route { get; set; }
}