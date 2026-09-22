using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("closed_circuit_lock_track_circuit")]
public class ClosedCircuitLockTrackCircuit
{
    [Key]
    public ulong Id { get; init; }

    [ForeignKey("Route")]
    public ulong RouteId { get; init; }

    [ForeignKey("TrackCircuit")]
    public ulong TrackCircuitId { get; init; }
}
