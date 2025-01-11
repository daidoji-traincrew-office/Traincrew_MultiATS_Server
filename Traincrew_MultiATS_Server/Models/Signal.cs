namespace Traincrew_MultiATS_Server.Models;

public class Signal
{
    public string TcName { get; set; }
    public virtual SignalState SignalState { get; set; }
}