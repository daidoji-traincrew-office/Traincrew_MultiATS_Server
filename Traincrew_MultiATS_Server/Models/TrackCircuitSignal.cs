using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("track_circuit_signal")]
public class TrackCircuitSignal
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public ulong Id { get; init; }
    
    [ForeignKey("TrackCircuit")]
    public ulong TrackCircuitId { get; init; }
    public TrackCircuit TrackCircuit { get; init; }

    public bool IsUp { get; init; }

    [ForeignKey("Signal")]
    public string SignalName { get; init; }
    public Signal Signal { get; init; }
}
