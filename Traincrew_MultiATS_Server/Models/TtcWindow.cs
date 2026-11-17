using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_window")]
public class TtcWindow
{
    [Key]
    [Column("name")]
    public string Name { get; init; }

    [Column("station_id")]
    public string StationId { get; init; }

    [Column("track_number")]
    public string TrackNumber { get; set; } = ""; // 番線(TTC経路の元番線/先番線と同じ表記)。番線の窓でなければ空文字

    [Column("type")]
    public TtcWindowType Type { get; init; }

    public TtcWindowState? TtcWindowState { get; set; }
}
