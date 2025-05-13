using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Models;

[Table("destination_button_state")]
public class DestinationButtonState
{
    [Key]
    public string Name { get; init; }
    public RaiseDrop IsRaised { get; set; }
   
    [Column(TypeName = "timestamp without time zone")]
    public DateTime OperatedAt { get; set; }
}
