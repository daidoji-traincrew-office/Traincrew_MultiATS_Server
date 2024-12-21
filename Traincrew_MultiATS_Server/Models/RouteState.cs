using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("route_state")]
public class RouteState
{
    public ulong Id { get; set; }
    public bool IsLeverReversed { get; set; }
    public bool IsReversed { get; set; }
    public bool ShouldReverse { get; set; }
    public virtual Route Route { get; set; }
}