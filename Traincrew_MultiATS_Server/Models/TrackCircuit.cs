using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("track_circuit")]
public class TrackCircuit : InterlockingObject
{
    public int ProtectionZone { get; set; }
    public TrackCircuitState? TrackCircuitState { get; set; }
}