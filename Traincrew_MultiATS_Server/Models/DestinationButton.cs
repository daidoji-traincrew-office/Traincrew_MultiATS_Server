using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("destination_button")]
public class DestinationButton
{
    [Key]
    public required string Name { get; init; }
    public string StationId { get; init; }
    public DestinationButtonState DestinationButtonState { get; init; }
}
