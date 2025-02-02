namespace Traincrew_MultiATS_Server.Models;

public class JsonTrackCircuitData : TrackCircuitData
{
    public List<string>? NextSignalNamesUp { get; init; } = null;
    public List<string>? NextSignalNamesDown { get; init; } = null;
}

public class JsonSignalData: SignalData
{
    public string TypeName { get; init; }
    public List<string>? NextSignalNames { get; init; } = null;
}


public class DBBasejson
{
    public List<JsonTrackCircuitData> trackCircuitList {get; set;}
    public List<JsonSignalData> signalDataList {get; set;}
    public List<SignalTypeData> signalTypeList { get; set; }
    public List<ProtectionZoneState> protectionZoneStateList {get; set;}
}