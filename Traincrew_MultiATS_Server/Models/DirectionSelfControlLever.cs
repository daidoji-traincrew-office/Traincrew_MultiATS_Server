using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("direction_self_control_lever")]
public class DirectionSelfControlLever : InterlockingObject
{
    public DirectionSelfControlLeverState? DirectionSelfControlLeverState { get; set; }

    internal override DirectionSelfControlLever ShallowClone() => (DirectionSelfControlLever)MemberwiseClone();

    /// <summary>
    /// マスタの複製に状態を載せたインスタンスを返す。
    /// </summary>
    /// <remarks>
    /// マスタのスナップショットはプロセス全体で共有されるため、共有インスタンスに
    /// <see cref="DirectionSelfControlLeverState"/> を直接代入するとリクエスト間でデータ競合する。
    /// 状態を載せるときは必ずこのメソッドを通し、複製に対して載せること。
    /// なお、ここで得た複製を IGeneralRepository.Save に渡してはならない
    /// (InterlockingObjectMaster の注意書きを参照)。
    /// </remarks>
    public DirectionSelfControlLever CloneWithState(DirectionSelfControlLeverState? state)
    {
        var clone = ShallowClone();
        clone.DirectionSelfControlLeverState = state;
        return clone;
    }
}