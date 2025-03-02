using CsvHelper.Configuration.Attributes;

namespace Traincrew_MultiATS_Server.Models;

public class RendoTableCSV
{
    [Index(0)]
    public string Name { get; set; }
    [Index(5)]
    public string Start { get; set; }
    [Index(6)]
    public string End { get; init; }
    [Index(7)]
    public string LockToSwitchingMachine { get; init; }
    [Index(8)]
    public string LockToRoute { get; init; }
    [Index(9)]
    public string SignalControl { get; init; }
    [Index(10)]
    public string RouteLock { get; init; }
    [Index(11)]
    public string ApproachLock { get; init; }
    [Index(12)]
    public string ApproachTime { get; set; }
}