using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("direction_lever")]
public class DirectionLever : InterlockingObject
{
    /// <summary>
    /// 物理てこのID
    /// </summary>
    public ulong LeverId { get; init; }

    /// <summary>
    /// 開放てこのID
    /// </summary>
    public ulong DirectionSelfControlLeverId { get; init; }

    /// <summary>
    /// Lてこに対する隣駅鎖錠てこ
    /// </summary>
    public ulong? LLockLeverId { get; init; }

    /// <summary>
    /// Lてこに対する隣駅鎖状てこの方向
    /// </summary>
    public LR? LLockLeverDirection { get; init; }

    /// <summary>
    /// Lてこに対する隣駅被片鎖状てこ
    /// </summary>
    public ulong? LSingleLockedLeverId { get; init; }

    /// <summary>
    /// Lてこに対する隣駅被片鎖状てこの方向
    /// </summary>
    public LR? LSingleLockedLeverDirection { get; init; }

    /// <summary>
    /// Rてこに対する隣駅鎖錠てこ
    /// </summary>
    public ulong? RLockLeverId { get; init; }

    /// <summary>
    /// Rてこに対する隣駅鎖状てこの方向
    /// </summary>
    public LR? RLockLeverDirection { get; init; }

    /// <summary>
    /// Rてこに対する隣駅被片鎖状てこ
    /// </summary>
    public ulong? RSingleLockedLeverId { get; init; }

    /// <summary>
    /// Rてこに対する隣駅被片鎖状てこの方向
    /// </summary>
    public LR? RSingleLockedLeverDirection { get; init; }

    public virtual DirectionLeverState? DirectionLeverState { get; init; }
}