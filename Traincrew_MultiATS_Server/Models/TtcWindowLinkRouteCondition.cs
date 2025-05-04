using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_window_link_route_condition")]
public class TtcWindowLinkRouteCondition
{
    [Column("id")]
    public long Id { get; set; }

    [Column("ttc_window_link_id")]
    public ulong TtcWindowLinkId { get; set; }

    [Column("route_id")]
    public ulong RouteId { get; set; }
}
