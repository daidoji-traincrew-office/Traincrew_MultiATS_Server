using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("destination_button_state")]
public class ButtonState
{
    [Key]
    public string Name { get; set; }
    public RaiseDrop IsRaised { get; set; }
    public DateTime OperatedAt { get; set; }
}
