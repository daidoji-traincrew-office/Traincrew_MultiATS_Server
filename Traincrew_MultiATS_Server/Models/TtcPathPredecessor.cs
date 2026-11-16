using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_path_predecessor")]
public class TtcPathPredecessor
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    [Column("id")]
    public ulong Id { get; init; }

    [Column("index")]
    public int Index { get; init; }

    [Column("source_ttc_path_id")]
    public ulong SourceTtcPathId { get; init; }

    public TtcPath? SourceTtcPath { get; init; }

    [Column("target_ttc_path_id")]
    public ulong TargetTtcPathId { get; init; }

    public TtcPath? TargetTtcPath { get; init; }
}
