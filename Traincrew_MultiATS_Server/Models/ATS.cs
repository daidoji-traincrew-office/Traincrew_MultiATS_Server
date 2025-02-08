public enum KokuchiType
{
    None,
    Yokushi,
    Tsuuchi,
    TsuuchiKaijo,
    Kaijo,
    Shuppatsu,
    ShuppatsuJikoku,
    Torikeshi,
    Tenmatsusho
}

public class KokuchiData
{
    public string DisplayData;
    public DateTime OriginTime;
    public KokuchiType Type;

    public KokuchiData(KokuchiType Type, string DisplayData, DateTime OriginTime)
    {
        this.Type = Type;
        this.DisplayData = DisplayData;
        this.OriginTime = OriginTime;
    }
}

public class EmergencyLightData
{
    public string Name;
    public bool State;
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

public class SignalData
{
    public string Name { get; init; }
    public Phase phase { get; init; } = Phase.None;

}

public class SignalTypeData
{
    public string Name { get; init; }
    public string RIndication { get; init; }
    public string YYIndication { get; init; }
    public string YIndication { get; init; }
    public string YGIndication { get; init; }
    public string GIndication { get; init; }
}

public class CarState
{
    public float Ampare;
    public float BC_Press;
    public string CarModel;
    public bool DoorClose;
    public bool HasConductorCab = false;
    public bool HasDriverCab = false;
    public bool HasMotor = false;
    public bool HasPantograph = false;
}

public class TrackCircuitData: IEquatable<TrackCircuitData>
{
    public string Last { get; init; } // 軌道回路を踏んだ列車の名前
    public required string Name { get; init; }
    public bool IsLocked { get; init; }
    public bool On { get; init; }

    public override string ToString()
    {
        return $"{Name}/{Last}/{On}";
    }

    public bool Equals(TrackCircuitData? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TrackCircuitData);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}

public class DataToServer
{
    public int BNotch { get; init; }
    public bool BougoState { get; init; }
    public List<CarState> CarStates { get; init; }
    public string DiaName { get; init; }
    public List<TrackCircuitData> OnTrackList { get; init; }

    public int PNotch{ get; init; }

    //将来用
    public float Speed{ get; init; }
}

public class DataFromServer
{
    //進路表示の表示はTC本体実装待ち　未決定
    public bool BougoState { get ; set; } = false;
    public List<EmergencyLightData> EmergencyLightDatas { get; set; } = [];
    public Dictionary<string, KokuchiData> KokuchiDatas { get; set; } = new();
    public List<SignalData> NextSignalData { get; set; } = [];
    public List<SignalData> DoubleNextSignalData { get; set; } = [];
}