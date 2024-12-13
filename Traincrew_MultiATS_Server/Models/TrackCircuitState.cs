namespace Traincrew_MultiATS_Server.Models;

public class TrackCircuitState
{
    public long Id { get; set; }
    public string? TrainNumber { get; set; }
    public bool IsShortCircuit { get; set; }
}