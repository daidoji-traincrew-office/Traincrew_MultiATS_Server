using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("direction_lever_direction")]
public class DirectionLeverDirection: InterlockingObject
{

    /// <summary>
    /// てこのID
    /// </summary>
    public ulong LeverId { get; set; }

    /// <summary>
    /// てこ
    /// </summary>
    public DirectionLever? Lever { get; set; } = null;

    /// <summary>
    /// 左右の方向
    /// </summary>
    [Column("is_lr")]
    public LR IsLR { get; set; }
}