using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Models.Enums;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_window")]
public class TtcWindow
{
    [Column("name")]
    public string Name { get; set; }

    [Column("station_id")]
    public string StationId { get; set; }

    [Column("type")]
    public TtcWindowType Type { get; set; }
}
