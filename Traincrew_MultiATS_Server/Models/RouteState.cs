using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Models;

[Table("route_state")]
public class RouteState
{
    [Key]
    public ulong Id { get; init; }
    /// <summary>
    /// てこ反応リレー
    /// </summary>
    public RaiseDrop IsLeverRelayRaised { get; set; }
    /// <summary>
    /// 進路照査リレー
    /// </summary>
    public RaiseDrop IsRouteRelayRaised { get; set; }
    /// <summary>
    /// 信号制御リレー
    /// </summary>
    public RaiseDrop IsSignalControlRaised { get; set; }
    /// <summary>
    /// 接近鎖錠リレー(MR)
    /// </summary>
    // ReSharper disable InconsistentNaming
    [Column("is_approach_lock_mr_raised")]
    public RaiseDrop IsApproachLockMRRaised { get; set; }
    /// <summary>
    /// 接近鎖錠リレー(MS)
    /// </summary>
    [Column("is_approach_lock_ms_raised")]
    public RaiseDrop IsApproachLockMSRaised { get; set; }
    /// <summary>
    /// 進路鎖錠リレー(実在しない)
    /// </summary>
    public RaiseDrop IsRouteLockRaised { get; set; }
    /// <summary>
    /// 総括反応リレー
    /// </summary>
    [Column("is_throw_out_xr_relay_raised")]
    public RaiseDrop IsThrowOutXRRelayRaised { get; set; }
    /// <summary>
    /// 総括反応中継リレー
    /// </summary>
    [Column("is_throw_out_ys_relay_raised")]
    public RaiseDrop IsThrowOutYSRelayRaised { get; set; }
    // ReSharper restore InconsistentNaming
}