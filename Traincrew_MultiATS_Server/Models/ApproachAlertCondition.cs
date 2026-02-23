using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("approach_alert_condition")]
public class ApproachAlertCondition
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public ulong Id { get; init; }
    public required string StationId { get; set; }
    public Station? Station { get; set; }
    public required bool IsUp { get; set; }
    public required ulong TrackCircuitId { get; set; }
    public TrackCircuit? TrackCircuit { get; set; }

    [Column("train_number_condition")]
    public BothOddEven TrainNumberCondition { get; set; } = BothOddEven.Both;
}
