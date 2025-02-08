using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("route_lever_destination_button")]
public class RouteLeverDestinationButton
{
    [Key]
    public ulong Id { get; set; }
    public long RouteId { get; set; }
    public string LeverName { get; set; }
    public string DestinationButtonName { get; set; }
}
