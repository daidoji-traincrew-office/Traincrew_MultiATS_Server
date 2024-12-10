using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("route")]
public class Route : InterlockingObject
{
    public string TcName { get; set; }
    public string? Description { get; set; }
    public string RouteType { get; set; }
    public string? Root { get; set; }
    public string? Indicator { get; set; }
    public int? ApproachLockTime { get; set; }
}