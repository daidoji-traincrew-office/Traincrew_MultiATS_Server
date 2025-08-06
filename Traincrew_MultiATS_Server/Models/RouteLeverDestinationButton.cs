using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("route_lever_destination_button")]
public class RouteLeverDestinationButton
{
    [Key]
    public ulong Id { get; set; }
    public ulong RouteId { get; set; }
    public ulong LeverId { get; set; }
    public Lever Lever { get; set; }
    public LR Direction { get; set; }
    public string? DestinationButtonName { get; set; }
    public DestinationButton? DestinationButton { get; set; }
}
