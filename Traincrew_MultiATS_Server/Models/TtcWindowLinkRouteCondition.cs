using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_window_link_route_condition")]
public class TtcWindowLinkRouteCondition
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    [Column("id")]
    public ulong Id { get; init; }

    [Column("ttc_window_link_id")]
    public ulong TtcWindowLinkId { get; init; }
    
    public TtcWindowLink TtcWindowLink { get; init; }

    [Column("route_id")]
    public ulong RouteId { get; init; }
}
