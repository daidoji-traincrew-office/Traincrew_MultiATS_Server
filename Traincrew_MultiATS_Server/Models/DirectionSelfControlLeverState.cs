using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("direction_self_control_lever_state")]
public class DirectionSelfControlLeverState
{
    /// <summary>
    /// てこのID
    /// </summary>
    [Key]
    public ulong Id { get; set; }

    /// <summary>
    /// 鍵が挿入されているか
    /// </summary>
    public bool IsInsertedKey { get; set; } = false;

    /// <summary>
    /// てこの位置
    /// </summary>
    public NR IsReversed { get; set; } = NR.Normal;
}
