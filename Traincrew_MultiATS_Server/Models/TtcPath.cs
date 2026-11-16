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
    public required string StationId { get; init; }

    [Column("source_track")]
    public required string SourceTrack { get; init; }

    [Column("target_track")]
    public required string TargetTrack { get; init; }

    [Column("type")]
    public TtcPathType Type { get; init; }

    // 順序断定回路数
    [Column("define_order_track_circuit_count")]
    public int DefineOrderTrackCircuitCount { get; init; }
}
