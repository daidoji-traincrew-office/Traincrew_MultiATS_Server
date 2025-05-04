using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_window_display_station")]
public class TtcWindowDisplayStation
{
    [Column("id")]
    public long Id { get; set; }

    [Column("ttc_window_name")]
    public string TtcWindowName { get; set; }

    [Column("station_id")]
    public string StationId { get; set; }
}
