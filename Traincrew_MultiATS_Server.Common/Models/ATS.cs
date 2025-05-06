namespace Traincrew_MultiATS_Server.Common.Models;

public class OperationNotificationData
{
    public string DisplayName { get; init; }
    public string Content { get; init; }
    public DateTime OperatedAt { get; init; }
    public OperationNotificationType Type { get; init; }
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

public class TrackCircuitData : IEquatable<TrackCircuitData>
{
    public string Last { get; init; } // 軌道回路を踏んだ列車の名前
    public required string Name { get; init; }
    public bool Lock { get; init; }
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

    public int PNotch { get; init; }

    //将来用
    public float Speed { get; init; }
}

public class RouteData
{
    public string TcName { get; set; }
    public RouteType RouteType { get; set; }
    public ulong? RootId { get; set; }
    public RouteData? Root { get; set; }
    public string? Indicator { get; set; }
    public int? ApproachLockTime { get; set; }
    public RouteStateData? RouteState { get; set; }
}

public class RouteStateData
{
    /// <summary>
    /// てこ反応リレー
    /// </summary>
    public RaiseDrop IsLeverRelayRaised { get; set; }

    /// <summary>
    /// 進路照査リレー
    /// </summary>
    public RaiseDrop IsRouteRelayRaised { get; set; }

    /// <summary>
    /// 信号制御リレー
    /// </summary>
    public RaiseDrop IsSignalControlRaised { get; set; }

    /// <summary>
    /// 接近鎖錠リレー(MR)
    /// </summary>
    public RaiseDrop IsApproachLockMRRaised { get; set; }

    /// <summary>
    /// 接近鎖錠リレー(MS)
    /// </summary>
    public RaiseDrop IsApproachLockMSRaised { get; set; }

    /// <summary>
    /// 進路鎖錠リレー(実在しない)
    /// </summary>
    public RaiseDrop IsRouteLockRaised { get; set; }

    /// <summary>
    /// 総括反応リレー
    /// </summary>
    public RaiseDrop IsThrowOutXRRelayRaised { get; set; }

    /// <summary>
    /// 総括反応中継リレー
    /// </summary>
    public RaiseDrop IsThrowOutYSRelayRaised { get; set; }
}

public class DataFromServer
{
    //進路表示の表示はTC本体実装待ち　未決定
    public bool BougoState { get; set; } = false;
    public List<EmergencyLightData> EmergencyLightDatas { get; set; } = [];
    public Dictionary<string, OperationNotificationData> KokuchiDatas { get; set; } = new();
    public OperationNotificationData? OperationNotificationData { get; set; } = null;
    public List<SignalData> NextSignalData { get; set; } = [];
    public List<SignalData> DoubleNextSignalData { get; set; } = [];
    public List<RouteData> RouteData { get; set; } = new();
}