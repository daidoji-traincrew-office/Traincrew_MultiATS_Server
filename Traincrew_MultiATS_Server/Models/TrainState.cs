using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("train_state")]
public class TrainState
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("train_number")]
    [Required]
    public required string TrainNumber { get; set; }

    [Column("dia_number")]
    public int DiaNumber { get; set; }

    [Column("from_station_id")]
    [Required]
    public required string FromStationId { get; set; }

    [Column("to_station_id")]
    [Required]
    public required string ToStationId { get; set; }

    [Column("delay")]
    public int Delay { get; set; }

    [Column("driver_id")]
    public long? DriverId { get; set; }
}
