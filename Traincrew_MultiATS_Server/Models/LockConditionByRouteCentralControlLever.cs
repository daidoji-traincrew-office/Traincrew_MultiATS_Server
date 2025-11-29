using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("lock_condition_by_route_central_control_lever")]
public class LockConditionByRouteCentralControlLever
{
    public ulong Id { get; set; }
    public ulong RouteId { get; set; }
    public ulong RouteCentralControlLeverId { get; set; }
}