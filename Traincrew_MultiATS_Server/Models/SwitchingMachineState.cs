namespace Traincrew_MultiATS_Server.Models;

public class SwitchingMachineState
{
    public long Id { get; set; }
    public bool IsReversed { get; set; }
    public bool? IsLeverReversed { get; set; }
    public DateTime? SwitchEndTime { get; set; }
}