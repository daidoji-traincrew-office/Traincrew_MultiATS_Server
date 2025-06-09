using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("train_state")]
public class TrainState
{
    [Key]
    [Column("train_number")]
    public string TrainNumber { get; set; }

    [Column("from_station_id")]
    public string FromStationId { get; set; }

    [Column("to_station_id")]
    public string ToStationId { get; set; }

    [Column("delay")]
    public int Delay { get; set; }

    [Column("driver_id")]
    public long? DriverId { get; set; }
}
