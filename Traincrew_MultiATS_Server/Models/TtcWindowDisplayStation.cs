using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_window_display_station")]
public class TtcWindowDisplayStation
{
    [Column("id")]
    public long Id { get; init; }

    [Column("ttc_window_name")]
    public string TtcWindowName { get; init; }

    [Column("station_id")]
    public string StationId { get; init; }
}
