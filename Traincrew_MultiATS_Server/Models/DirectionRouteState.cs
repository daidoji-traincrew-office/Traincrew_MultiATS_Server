using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Common.Models;

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

    /// <summary>
    /// 運転方向鎖錠リレー
    /// </summary>
    public RaiseDrop IsFlRelayRaised { get; set; } = RaiseDrop.Drop;

    /// <summary>
    /// L方向総括リレー
    /// </summary>
    public RaiseDrop IsLfysRelayRaised { get; set; } = RaiseDrop.Drop;

    /// <summary>
    /// R方向総括リレー
    /// </summary>
    public RaiseDrop IsRfysRelayRaised { get; set; } = RaiseDrop.Drop;

    /// <summary>
    /// L方向てこ反応リレー
    /// </summary>
    public RaiseDrop IsLyRelayRaised { get; set; } = RaiseDrop.Drop;

    /// <summary>
    /// R方向てこ反応リレー
    /// </summary>
    public RaiseDrop IsRyRelayRaised { get; set; } = RaiseDrop.Drop;

    /// <summary>
    /// L方向てこリレー
    /// </summary>
    public RaiseDrop IsLRelayRaised { get; set; } = RaiseDrop.Drop;

    /// <summary>
    /// R方向てこリレー
    /// </summary>
    public RaiseDrop IsRRelayRaised { get; set; } = RaiseDrop.Drop;
}