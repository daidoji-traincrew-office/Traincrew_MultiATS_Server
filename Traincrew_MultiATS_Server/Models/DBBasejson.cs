namespace Traincrew_MultiATS_Server.Models;

public class DBBasejson
{
    public List<TrackCircuitData> trackCircuitList {get; set;}
    public List<SignalData> signalDataList {get; set;}
    public List<ProtectionZoneState> protectionZoneStateList {get; set;}
}