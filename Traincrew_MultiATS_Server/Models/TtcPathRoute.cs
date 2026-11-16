using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("ttc_path_route")]
public class TtcPathRoute
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    [Column("id")]
    public ulong Id { get; init; }

    [Column("index")]
    public int Index { get; init; }

    [Column("ttc_path_id")]
    public ulong TtcPathId { get; init; }

    public TtcPath? TtcPath { get; init; }

    [Column("route_id")]
    public ulong RouteId { get; init; }
}
