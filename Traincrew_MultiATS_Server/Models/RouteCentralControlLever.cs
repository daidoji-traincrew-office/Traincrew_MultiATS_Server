using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("route_central_control_lever")]
public class RouteCentralControlLever : InterlockingObject
{
    public RouteCentralControlLeverState? RouteCentralControlLeverState { get; set; }

    internal override RouteCentralControlLever ShallowClone() => (RouteCentralControlLever)MemberwiseClone();

    /// <summary>
    /// マスタの複製に状態を載せたインスタンスを返す。
    /// </summary>
    /// <remarks>
    /// マスタのスナップショットはプロセス全体で共有されるため、共有インスタンスに
    /// <see cref="RouteCentralControlLeverState"/> を直接代入するとリクエスト間でデータ競合する。
    /// 状態を載せるときは必ずこのメソッドを通し、複製に対して載せること。
    /// なお、ここで得た複製を IGeneralRepository.Save に渡してはならない
    /// (InterlockingObjectMaster の注意書きを参照)。
    /// </remarks>
    public RouteCentralControlLever CloneWithState(RouteCentralControlLeverState? state)
    {
        var clone = ShallowClone();
        clone.RouteCentralControlLeverState = state;
        return clone;
    }
}