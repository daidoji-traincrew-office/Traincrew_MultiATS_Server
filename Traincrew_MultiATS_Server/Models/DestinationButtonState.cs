using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("destination_button_state")]
public class DestinationButtonState
{
    [Key]
    public required string Name { get; init; }
    public RaiseDrop IsRaised { get; set; }
    public DateTime OperatedAt { get; set; }
}
