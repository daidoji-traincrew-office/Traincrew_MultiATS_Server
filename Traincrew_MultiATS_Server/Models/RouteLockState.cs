namespace Traincrew_MultiATS_Server.Models;

public class RouteLockState
{
    public long Id { get; set; }
    public int TargetRouteId { get; set; }
    public int SourceRouteId { get; set; }
    public string LockType { get; set; }
    public DateTime? EndTime { get; set; }
}