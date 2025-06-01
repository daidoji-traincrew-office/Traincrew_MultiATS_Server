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

    [Column("type")]
    public TtcWindowType Type { get; init; }

    public TtcWindowState? TtcWindowState { get; set; }
}
