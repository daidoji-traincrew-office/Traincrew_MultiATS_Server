using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("train_diagram")]
public class TrainDiagram
{
    [Key]
    [Column("train_number")]
    public string TrainNumber { get; set; }

    [Required]
    [Column("type_id")]
    public long TypeId { get; set; }

    [ForeignKey(nameof(TypeId))]
    public TrainType? TrainType { get; set; }

    [Required]
    [Column("from_station_id")]
    public string FromStationId { get; set; }

    [Required]
    [Column("to_station_id")]
    public string ToStationId { get; set; }

    [Required]
    [Column("dia_id")]
    public int DiaId { get; set; }
}
