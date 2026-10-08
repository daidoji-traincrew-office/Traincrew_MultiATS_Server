using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("switching_machine")]
public class SwitchingMachine : InterlockingObject
{
    public string TcName { get; set; }
    public virtual SwitchingMachineState SwitchingMachineState { get; set; }

    internal override SwitchingMachine ShallowClone() => (SwitchingMachine)MemberwiseClone();

    /// <summary>
    /// マスタの複製に状態を載せたインスタンスを返す。
    /// </summary>
    /// <remarks>
    /// マスタのスナップショットはプロセス全体で共有されるため、共有インスタンスに
    /// <see cref="SwitchingMachineState"/> を直接代入するとリクエスト間でデータ競合する。
    /// 状態を載せるときは必ずこのメソッドを通し、複製に対して載せること。
    /// なお、ここで得た複製を IGeneralRepository.Save に渡してはならない
    /// (InterlockingObjectMaster の注意書きを参照)。
    /// </remarks>
    public SwitchingMachine CloneWithState(SwitchingMachineState state)
    {
        var clone = ShallowClone();
        clone.SwitchingMachineState = state;
        return clone;
    }
}