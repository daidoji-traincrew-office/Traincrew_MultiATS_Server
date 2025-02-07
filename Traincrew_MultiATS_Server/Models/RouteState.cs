using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("route_state")]
public class RouteState
{
    [Key]
    public ulong Id { get; init; }
    public bool IsLeverRelayRaised { get; set; }
    public bool IsRouteRelayRaised { get; set; }
    public bool IsSignalControlRaised { get; set; }
    public bool IsApproachLockRaised { get; set; }
    public bool IsRouteLockRaised { get; set; }
    public Route? Route { get; set; }
}