using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Models;

[Table("route_central_control_lever_state")]
public class RouteCentralControlLeverState
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

    /// <summary>
    /// chrリレー扛上しているか
    /// </summary>
    public RaiseDrop IsChrRelayRaised { get; set; } = RaiseDrop.Drop;
}