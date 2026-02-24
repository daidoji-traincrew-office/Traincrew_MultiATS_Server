using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("approach_alert_state")]
public class ApproachAlertState
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public ulong Id { get; init; }
    public required string StationId { get; set; }
    public required bool IsUp { get; set; }
    public bool ShouldRing { get; set; }
    public bool IsRinging { get; set; }
}
