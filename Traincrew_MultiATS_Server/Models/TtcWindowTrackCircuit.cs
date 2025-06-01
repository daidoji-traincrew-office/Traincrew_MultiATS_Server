using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_window_track_circuit")]
public class TtcWindowTrackCircuit
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    [Column("id")]
    public ulong id { get; init; }

    [Column("ttc_window_name")]
    public string TtcWindowName { get; init; }

    [Column("track_circuit_id")]
    public ulong TrackCircuitId { get; init; }
}
