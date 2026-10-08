using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Models;

[Table("route")]
public class Route : InterlockingObject
{
    public string TcName { get; set; }
    public RouteType RouteType { get; set; }
    public ulong? RootId { get; set; }
    public Route? Root { get; set; }
    public string? Indicator { get; set; }
    public int? ApproachLockTime { get; set; }
    public RouteState? RouteState { get; set; }
    public ulong? ApproachLockFinalTrackCircuitId { get; set; }

    internal override Route ShallowClone() => (Route)MemberwiseClone();

    /// <summary>
    /// マスタの複製に状態を載せたインスタンスを返す。
    /// </summary>
    /// <remarks>
    /// マスタのスナップショットはプロセス全体で共有されるため、共有インスタンスに
    /// <see cref="RouteState"/> を直接代入するとリクエスト間でデータ競合する。
    /// 状態を載せるときは必ずこのメソッドを通し、複製に対して載せること。
    /// なお、ここで得た複製を IGeneralRepository.Save に渡してはならない
    /// (InterlockingObjectMaster の注意書きを参照)。
    /// </remarks>
    public Route CloneWithState(RouteState? state)
    {
        var clone = ShallowClone();
        clone.RouteState = state;
        return clone;
    }
}