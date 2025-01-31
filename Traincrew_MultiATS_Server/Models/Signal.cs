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
    public SignalType Type { get; init; }
    // Todo: signal_stateの追加
    public ulong? TrackCircuitId { get; init; }
    public TrackCircuit? TrackCircuit { get; init; } 
    public Route? Route { get; init; }
}