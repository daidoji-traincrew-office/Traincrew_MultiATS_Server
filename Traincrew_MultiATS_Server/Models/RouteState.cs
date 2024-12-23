using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("route_state")]
public class RouteState
{
    public ulong Id { get; set; }
    public NR IsLeverReversed { get; set; }
    public NR IsReversed { get; set; }
    public NR ShouldReverse { get; set; }
    public virtual Route Route { get; set; }
}