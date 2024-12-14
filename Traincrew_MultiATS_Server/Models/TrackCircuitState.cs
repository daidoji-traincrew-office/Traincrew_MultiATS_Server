using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("track_circuit_state")]
public class TrackCircuitState
{
    public ulong Id { get; set; }
    public string? TrainNumber { get; set; }
    public bool IsShortCircuit { get; set; }
    public virtual TrackCircuit TrackCircuit { get; set; }
}