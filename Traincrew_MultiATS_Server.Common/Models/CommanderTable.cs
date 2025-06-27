namespace Traincrew_MultiATS_Server.Common.Models;

public enum TroubleType
{
}

public enum PlaceType
{
    // 軌道回路
    TrackCircuit,

    // 踏切
    Crossing,

    // 車両
    Train,

    // 駅ホーム
    Platform
}

public class TroubleData
{
    public TroubleType TroubleType { get; set; }
    public PlaceType PlaceType { get; set; }
    public string PlaceName { get; set; }
    public DateTime OccuredAt { get; set; }
    public string AdditionalData { get; set; }
}

public class DataToCommanderTable
{
    public List<TroubleData> TroubleDataList { get; set; }
    public List<OperationNotificationData> OperationNotificationDataList { get; set; }
    public List<TrackCircuitData> TrackCircuitDataList { get; set; }
}

public class ProtectionZoneData
{
    public ulong Id { get; set; }
    public string TrainNumber { get; set; }
    public int ProtectionZone { get; set; }
}