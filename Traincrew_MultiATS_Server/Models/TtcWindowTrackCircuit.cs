using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_window_track_circuit")]
public class TtcWindowTrackCircuit
{
    [Column("id")]
    public long Id { get; init; }

    [Column("ttc_window_name")]
    public string TtcWindowName { get; init; }

    [Column("track_circuit_id")]
    public ulong TrackCircuitId { get; init; }
}
