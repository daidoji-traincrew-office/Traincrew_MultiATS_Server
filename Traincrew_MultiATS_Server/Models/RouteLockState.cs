using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

public class RouteLockState
{
    public long Id { get; set; }
    public int TargetRouteId { get; set; }
    public int SourceRouteId { get; set; }
    public string LockType { get; set; }
    [Column(TypeName = "timestamp without time zone")]
    public DateTime? EndTime { get; set; }
}