using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("diagram_train_timetable")]
public class DiagramTrainTimetable
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }

    [Required]
    [Column("train_diagram_id")]
    public ulong TrainDiagramId { get; set; }

    [Required]
    [Column("index")]
    public int Index { get; set; }

    [Required]
    [Column("station_id")]
    public string StationId { get; set; } = string.Empty;

    [Required]
    [Column("track_number")]
    public string TrackNumber { get; set; } = string.Empty;

    [Column("arrival_time")]
    public TimeSpan? ArrivalTime { get; set; }

    [Column("departure_time")]
    public TimeSpan? DepartureTime { get; set; }

    // ナビゲーションプロパティ
    [ForeignKey(nameof(TrainDiagramId))]
    public DiagramTrain? TrainDiagram { get; set; }

    [ForeignKey(nameof(StationId))]
    public Station? Station { get; set; }
}
