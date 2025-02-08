using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("destination_button")]
public class Button
{
    [Key]
    public string Name { get; set; }
    public string StationId { get; set; }
    public ButtonState ButtonState { get; set; }
}
