using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("lever")]
public class Lever: InterlockingObject
{
    public LeverType LeverType { get; init; }
    public ulong? SwitchingMachineId { get; init; }
    public SwitchingMachine? SwitchingMachine { get; init; }
    public LeverState LeverState { get; set; }

    internal override Lever ShallowClone() => (Lever)MemberwiseClone();

    /// <summary>
    /// マスタの複製に状態を載せたインスタンスを返す。
    /// </summary>
    /// <remarks>
    /// マスタのスナップショットはプロセス全体で共有されるため、共有インスタンスに
    /// <see cref="LeverState"/> を直接代入するとリクエスト間でデータ競合する。
    /// 状態を載せるときは必ずこのメソッドを通し、複製に対して載せること。
    /// なお、ここで得た複製を IGeneralRepository.Save に渡してはならない
    /// (InterlockingObjectMaster の注意書きを参照)。
    /// </remarks>
    public Lever CloneWithState(LeverState state)
    {
        var clone = ShallowClone();
        clone.LeverState = state;
        return clone;
    }
}
