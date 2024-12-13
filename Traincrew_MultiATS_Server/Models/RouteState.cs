namespace Traincrew_MultiATS_Server.Models;

public class RouteState
{
    public long Id { get; set; }
    public bool IsLeverReversed { get; set; }
    public bool IsReversed { get; set; }
    public bool ShouldReverse { get; set; }
}