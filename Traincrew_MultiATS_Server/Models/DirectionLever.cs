using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("direction_lever")]
public class DirectionLever: InterlockingObject
{
    /// <summary>
    /// Lてこに対する隣駅鎖錠てこ
    /// </summary>
    public ulong? LLockLeverId { get; set; }

    /// <summary>
    /// Lてこに対する隣駅被片鎖状てこ
    /// </summary>
    public ulong? LSingleLockedLeverId { get; set; }

    /// <summary>
    /// Rてこに対する隣駅鎖錠てこ
    /// </summary>
    public ulong? RLockLeverId { get; set; }

    /// <summary>
    /// Rてこに対する隣駅被片鎖状てこ
    /// </summary>
    public ulong? RSingleLockedLeverId { get; set; }

    public virtual DirectionLeverState? DirectionLeverState { get; set; }
}