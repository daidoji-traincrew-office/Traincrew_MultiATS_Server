using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("direction_route")]
public class DirectionRoute : InterlockingObject
{
    /// <summary>
    /// 物理てこのID
    /// </summary>
    public ulong LeverId { get; init; }
    
    /// <summary>
    /// 物理てこ
    /// </summary>
    public Lever? Lever { get; init; }

    /// <summary>
    /// 開放てこのID
    /// </summary>
    public ulong? DirectionSelfControlLeverId { get; set; }

    /// <summary>
    /// Lてこに対する隣駅鎖錠てこ
    /// </summary>
    public ulong? LLockLeverId { get; set; }

    /// <summary>
    /// Lてこに対する隣駅鎖状てこの方向
    /// </summary>
    public LR? LLockLeverDirection { get; set; }

    /// <summary>
    /// Lてこに対する隣駅被片鎖状てこ
    /// </summary>
    public ulong? LSingleLockedLeverId { get; set; }

    /// <summary>
    /// Lてこに対する隣駅被片鎖状てこの方向
    /// </summary>
    public LR? LSingleLockedLeverDirection { get; set; }

    /// <summary>
    /// Rてこに対する隣駅鎖錠てこ
    /// </summary>
    public ulong? RLockLeverId { get; set; }

    /// <summary>
    /// Rてこに対する隣駅鎖状てこの方向
    /// </summary>
    public LR? RLockLeverDirection { get; set; }

    /// <summary>
    /// Rてこに対する隣駅被片鎖状てこ
    /// </summary>
    public ulong? RSingleLockedLeverId { get; set; }

    /// <summary>
    /// Rてこに対する隣駅被片鎖状てこの方向
    /// </summary>
    public LR? RSingleLockedLeverDirection { get; set; }

    public virtual DirectionRouteState? DirectionRouteState { get; set; }

    internal override DirectionRoute ShallowClone() => (DirectionRoute)MemberwiseClone();

    /// <summary>
    /// マスタの複製に状態を載せたインスタンスを返す。
    /// </summary>
    /// <remarks>
    /// マスタのスナップショットはプロセス全体で共有されるため、共有インスタンスに
    /// <see cref="DirectionRouteState"/> を直接代入するとリクエスト間でデータ競合する。
    /// 状態を載せるときは必ずこのメソッドを通し、複製に対して載せること。
    /// なお、ここで得た複製を IGeneralRepository.Save に渡してはならない
    /// (InterlockingObjectMaster の注意書きを参照)。
    /// </remarks>
    public DirectionRoute CloneWithState(DirectionRouteState? state)
    {
        var clone = ShallowClone();
        clone.DirectionRouteState = state;
        return clone;
    }
}