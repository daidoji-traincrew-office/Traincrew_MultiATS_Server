using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("protection_zone_state")]
public class ProtectionZoneState
{
    [Key]
	public ulong ProtectionZone{ get; set; }
	public List<string>? TrainNumber { get; set; }
}