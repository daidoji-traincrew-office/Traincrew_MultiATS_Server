using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_window_link")]
public class TtcWindowLink
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    [Column("id")]
    public ulong Id { get; init; }

    [Column("source_ttc_window_name")]
    public string SourceTtcWindowName { get; init; }

    [Column("target_ttc_window_name")]
    public string TargetTtcWindowName { get; init; }

    [Column("type")]
    public TtcWindowLinkType Type { get; init; }

    [Column("is_empty_sending")]
    public bool IsEmptySending { get; init; }

    [Column("track_circuit_condition")]
    public ulong? TrackCircuitCondition { get; init; }
}
