using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("route_central_control_lever")]
public class RouteCentralControlLever : InterlockingObject
{
    public RouteCentralControlLeverState? RouteCentralControlLeverState { get; set; }
}