namespace Traincrew_MultiATS_Server.Models;

public class JsonTrackCircuitData : TrackCircuitData
{
    public int? ProtectionZone { get; init; } = null;
    public List<string> NextSignalNamesUp { get; init; } = [];
    public List<string> NextSignalNamesDown { get; init; } = [];
}

public class JsonSignalData : SignalData
{
    public string TypeName { get; init; }
    public List<string>? NextSignalNames { get; init; } = null;
    public List<string>? RouteNames { get; init; } = null;
}

public class DBBasejson
{
    public List<Station> stationList { get; set; }
    public List<JsonTrackCircuitData> trackCircuitList { get; set; }
    public List<JsonSignalData> signalDataList { get; set; }
    public List<SignalTypeData> signalTypeList { get; set; }
}