using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_window_link_route_condition")]
public class TtcWindowLinkRouteCondition
{
    [Column("id")]
    public long Id { get; init; }

    [Column("ttc_window_link_id")]
    public ulong TtcWindowLinkId { get; init; }

    [Column("route_id")]
    public ulong RouteId { get; init; }
}
