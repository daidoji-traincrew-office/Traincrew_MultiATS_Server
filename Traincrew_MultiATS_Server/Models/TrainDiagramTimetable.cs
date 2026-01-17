using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("train_diagram_timetable")]
public class TrainDiagramTimetable
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("train_number")]
    public string TrainNumber { get; set; } = string.Empty;

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
    [ForeignKey(nameof(TrainNumber))]
    public TrainDiagram? TrainDiagram { get; set; }

    [ForeignKey(nameof(StationId))]
    public Station? Station { get; set; }
}