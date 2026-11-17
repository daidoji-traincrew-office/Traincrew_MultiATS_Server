using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

/// <summary>
/// 競合する進路の組(route_id と conflicting_route_id は同時に成立しない)
/// </summary>
[Table("route_conflict")]
public class RouteConflict
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    [Column("id")]
    public ulong Id { get; init; }

    [Column("route_id")]
    public ulong RouteId { get; init; }

    public Route? Route { get; init; }

    [Column("conflicting_route_id")]
    public ulong ConflictingRouteId { get; init; }

    public Route? ConflictingRoute { get; init; }
}
