using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("signal")]
public class Signal
{
    [Key]
    public string Name { get; init; }
    [Column("type")]
    public string TypeName { get; init; }
    public string? StationId { get; init; }
    public SignalType Type { get; init; }
    public SignalState SignalState { get; init; }
    public ulong? TrackCircuitId { get; init; }
    public TrackCircuit? TrackCircuit { get; init; } 
    public DirectionRoute? DirectionRouteLeft { get; init; }
    public DirectionRoute? DirectionRouteRight { get; init; }
    public LR? Direction { get; init; }
}