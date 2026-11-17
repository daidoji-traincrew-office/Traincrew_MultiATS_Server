using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_path")]
public class TtcPath
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    [Column("id")]
    public ulong Id { get; init; }

    [Column("name")]
    public required string Name { get; init; }

    [Column("station_id")]
    public required string StationId { get; set; }

    [Column("source_track")]
    public required string SourceTrack { get; set; }

    [Column("target_track")]
    public required string TargetTrack { get; set; }

    [Column("type")]
    public TtcPathType Type { get; set; }

    // 順序断定回路数
    [Column("define_order_track_circuit_count")]
    public int DefineOrderTrackCircuitCount { get; set; }
}
