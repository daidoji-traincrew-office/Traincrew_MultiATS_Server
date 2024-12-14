using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("switching_machine_state")]
public class SwitchingMachineState
{
    public ulong Id { get; set; }
    public bool IsReversed { get; set; }
    public bool? IsLeverReversed { get; set; }
    public DateTime? SwitchEndTime { get; set; }
    public virtual SwitchingMachine SwitchingMachine { get; set; }
}