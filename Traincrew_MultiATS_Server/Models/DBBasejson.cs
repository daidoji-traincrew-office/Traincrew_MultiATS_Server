using Traincrew_MultiATS_Server.Common.Models;

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
    public string? DirectionRouteLeft { get; init; } = null;
    public string? DirectionRouteRight { get; init; } = null;
    public string? Direction { get; init; } = null;
    public string? TrackCircuitName { get; init; } = null;
}

public class ThrowOutControlData
{
    public string SourceRouteName { get; init; } = "";
    public string TargetRouteName { get; init; } = "";
    public string LeverConditionName { get; init; } = "";
}

public class DBBasejson
{
    public List<Station> stationList { get; set; }
    public List<JsonTrackCircuitData> trackCircuitList { get; set; }
    public List<JsonSignalData> signalDataList { get; set; }
    public List<SignalTypeData> signalTypeList { get; set; }
    public List<ThrowOutControlData> throwOutControlList { get; set; }
}