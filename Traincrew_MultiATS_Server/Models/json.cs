namespace Traincrew_MultiATS_Server.Models;

public class DBBasejson
{
    public List<TrackCircuitData> trackCircuitList {get; set;}
    public List<SignalData> signalDataList {get; set;}
}

public class TrackCircuitData
{
    public bool On {get; set;}
    public string Last {get; set;}//軌道回路を踏んだ列車の名前
    public string Name {get; set;}

    public override string ToString()
    {
        return $"{Name}/{Last}/{On}";
    }
}
public class SignalData
{
    public string Name {get; set;}
    public Phase phase {get; set;}
}

public enum Phase
{
    None,
    R,
    YY,
    Y,
    YG,
    G
}
