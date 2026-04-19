using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("diagram_train")]
public class DiagramTrain
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }

    [Required]
    [Column("train_number")]
    public string TrainNumber { get; set; } = string.Empty;

    [Required]
    [Column("train_type_id")]
    public long TrainTypeId { get; set; }

    [ForeignKey(nameof(TrainTypeId))]
    public TrainType? TrainType { get; set; }

    [Required]
    [Column("from_station_id")]
    public string FromStationId { get; set; } = string.Empty;

    [Required]
    [Column("to_station_id")]
    public string ToStationId { get; set; } = string.Empty;

    [Required]
    [Column("dia_id")]
    public ulong DiaId { get; set; }

    [ForeignKey(nameof(DiaId))]
    public Diagram? Diagram { get; set; }
}
