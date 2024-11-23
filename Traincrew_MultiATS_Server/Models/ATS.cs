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
    public KokuchiType Type;
    public string DisplayData;
    public DateTime OriginTime;

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
    public string Name;
    public Phase phase = Phase.None;
}

public class CarState
{
    public bool DoorClose;
    public float BC_Press;
    public float Ampare;
    public string CarModel;
    public bool HasPantograph = false;
    public bool HasDriverCab = false;
    public bool HasConductorCab = false;
    public bool HasMotor = false;
}

public class TrackCircuitData
{
    public bool On = false;
    public string Last = null;//軌道回路を踏んだ列車の名前
    public string Name = "";

    public override string ToString()
    {
        return $"{Name}/{Last}/{On}";
    }
}

public class DataToServer
{
    public string DiaName;
    public List<TrackCircuitData> OnTrackList = null;
    public bool BougoState;
    //将来用
    public float Speed;
    public int PNotch;
    public int BNotch;
    public List<CarState> CarStates = new List<CarState>();
}

public class DataFromServer
{
    public SignalData NextSignalData = null;
    public SignalData DoubleNextSignalData = null;
    //進路表示の表示はTC本体実装待ち　未決定
    public bool BougoState;
    public List<EmergencyLightData> EmergencyLightDatas;
    public Dictionary<string, KokuchiData> KokuchiData;
}