namespace Traincrew_MultiATS_Server.Models;

[Serializable]
public class TTC_Data
{
    public List<TTC_Train> trainList = [];
    // public List<TTC_Station> stationList = new List<TTC_Station>();

}

/// <summary>
/// 列車ごとの情報
/// </summary>
[Serializable]
public class TTC_Train
{
    public int operationNumber; //運用番号
    public string trainNumber; //列車番号
    public string previousTrainNumber; //前列番
    public string nextTrainNumber; //後列番
    public string trainClass; //列車種別
    public string originStationID; //始発駅
    public string originStationName; //始発駅
    public string destinationStationID; //終着駅
    public string destinationStationName; //終着駅
    public List<TTC_StationData> staList = [];
    public string[] temporaryStopStations; //臨時停車駅
    public bool isRegularService = true; //定期/不定期判定
    public int carCount = 4; //車両数
}

/// <summary>
/// 停車駅の情報
/// </summary>
[Serializable]
public class TTC_StationData
{
    public string stationID = "";
    public string stationName;
    public string stopPosName;
    public TimeOfDay arrivalTime;
    public TimeOfDay departureTime;
}

/// <summary>
/// 時刻の情報
/// </summary>
public sealed class TimeOfDay
{
    public int h { get; set; }
    public int m { get; set; }
    public int s { get; set; }
}
