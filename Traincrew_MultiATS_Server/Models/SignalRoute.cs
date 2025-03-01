using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("signal_route")]
public class SignalRoute
{
    public long RouteId { get; set; }
    public string SignalName { get; set; }

    public Route Route { get; set; }
}
