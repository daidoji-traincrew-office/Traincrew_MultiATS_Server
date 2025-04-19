using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("next_signal")]
public class NextSignal
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key] 
    public ulong Id { get; init; }
    public required string SignalName { get; init; }
    public required string SourceSignalName { get; init; }
    public required string TargetSignalName { get; init; }
    public int Depth { get; init; }
}