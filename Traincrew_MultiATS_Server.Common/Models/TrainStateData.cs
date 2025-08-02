namespace Traincrew_MultiATS_Server.Common.Models;

public class TrainStateData
{
    public long Id { get; init; }
    public string TrainNumber { get; init; } = string.Empty;
    public int DiaNumber { get; init; }
    public string FromStationId { get; init; } = string.Empty;
    public string ToStationId { get; init; } = string.Empty;
    public int Delay { get; init; }
    public ulong? DriverId { get; init; }
}
