// ReSharper disable InconsistentNaming

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("signal_type")]
public class SignalType
{
    [Key] 
    public required string Name { get; init; }
    [Column("r_indication")]
    public required SignalIndication RIndication { get; init; }
    [Column("yy_indication")]
    public required SignalIndication YYIndication { get; init; }
    [Column("y_indication")]
    public required SignalIndication YIndication { get; init; }
    [Column("yg_indication")]
    public required SignalIndication YGIndication { get; init; }
    [Column("g_indication")]
    public required SignalIndication GIndication { get; init; }
}