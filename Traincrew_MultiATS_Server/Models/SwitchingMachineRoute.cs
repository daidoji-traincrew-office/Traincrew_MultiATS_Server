using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("switching_machine_route")]
public class SwitchingMachineRoute
{
    [Key]
    public ulong Id { get; set; }
    public ulong SwitchingMachineId { get; set; }
    public ulong RouteId { get; set; }
    public NR IsReverse { get; set; }
    public bool OnRouteLock { get; set; }
}
