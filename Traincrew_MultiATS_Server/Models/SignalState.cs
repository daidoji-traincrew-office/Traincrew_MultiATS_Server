using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("signal_state")]
public class SignalState
{
    [Key]
    public string SignalName { get; init; }
    public bool IsLighted { get; set; }
}