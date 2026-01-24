using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("track_circuit_department_time")]
public class TrackCircuitDepartmentTime
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("track_circuit_id")]
    public ulong TrackCircuitId { get; set; }

    [Column("car_count")]
    public int CarCount { get; set; }

    [Column("time_element")]
    public int TimeElement { get; set; }

    [Column("is_up")]
    public bool IsUp { get; set; }

    // ナビゲーションプロパティ
    [ForeignKey(nameof(TrackCircuitId))]
    public TrackCircuit? TrackCircuit { get; set; }
}
