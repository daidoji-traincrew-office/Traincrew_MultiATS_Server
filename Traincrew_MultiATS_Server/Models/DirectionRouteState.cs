using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("direction_route_state")]
public class DirectionRouteState
{
    /// <summary>
    /// ID
    /// </summary>
    [Key]
    public ulong Id { get; init; }

    /// <summary>
    /// 方向てこの方向
    /// </summary>
    public LR isLr { get; set; } = LR.Left;
}
