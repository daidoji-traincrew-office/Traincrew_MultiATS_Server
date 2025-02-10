using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("lever")]
public class Lever: InterlockingObject
{
    public LeverType LeverType { get; init; }
    public ulong? SwitchingMachineId { get; init; }
    public SwitchingMachine SwitchingMachine { get; init; }
    public LeverState LeverState { get; init; }
}
