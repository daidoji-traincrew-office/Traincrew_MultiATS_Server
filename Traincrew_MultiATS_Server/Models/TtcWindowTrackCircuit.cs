using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_window_track_circuit")]
public class TtcWindowTrackCircuit
{
    [Column("id")]
    public long Id { get; set; }

    [Column("ttc_window_name")]
    public string TtcWindowName { get; set; }

    [Column("track_circuit_id")]
    public ulong TrackCircuitId { get; set; }
}
