using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_window_display_station")]
public class TtcWindowDisplayStation
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    [Column("id")]
    public ulong id { get; init; }

    [Column("ttc_window_name")]
    public string TtcWindowName { get; init; }

    [Column("station_id")]
    public string StationId { get; init; }
}
