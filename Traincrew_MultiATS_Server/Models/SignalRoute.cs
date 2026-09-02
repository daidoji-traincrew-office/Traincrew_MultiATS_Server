using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("signal_route")]
public class SignalRoute
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public ulong Id { get; set; }
    public ulong RouteId { get; set; }
    public string SignalName { get; set; }

    public Route Route { get; set; }
}
