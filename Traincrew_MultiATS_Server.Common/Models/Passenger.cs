namespace Traincrew_MultiATS_Server.Common.Models;

public class TrainInfo
{
    public string Name { get; init; }
    public List<CarState> CarStates { get; init; } = [];
    public int TrainClass { get; init; }
    public string FromStation { get; init; }
    public string DestinationStation { get; init; }
    public int Delay { get; init; }
}


public class ServerToPassengerData
{
    public List<TrackCircuitData> TrackCircuitData { get; init; }
    public Dictionary<string, TrainInfo> TrainInfos { get; init; } = [];
    public List<OperationInformationData> OperationInformations { get; init; } = [];
}

/*
root(object)
1."TrackCircuits"(List<TrackCircuitData>)　在線している軌道回路情報の配列
  1.1.TrackCircuitData(object)　軌道回路情報
    1.1.1."Name"(string)　軌道回路名
    1.1.2."Last"(string)　列番
    1.1.3."On"(bool)　短絡しているか(互換性保持)
    1.1.4."Lock"(bool)　鎖錠しているか(互換性保持)
2."TrainInfos"(Dictionary<TrackCircuitData>)
  2.1.key(string)　列番(キー)
  2.2.TrainInfo(object)　列車情報
    2.2.1.Name(stirng)　列番
    2.2.2.CarStates(List<CarState>)　車両情報
      2.2.2.1.CarModel(stirng)　車両形式
      2.2.2.2.HasPantograph(bool)　パンタありなし
      2.2.2.3.HasDriverCab(bool)　運転台ありなし
      2.2.2.4.HasConductorCab(bool)　車掌室ありなし
      2.2.2.5.HasMotor(bool)　電動機ありなし
      2.2.2.6.DoorClose(bool)　戸閉(互換性保持)
      2.2.2.7.BC_Press(float)　BC圧力
      2.2.2.8.Ampare(float)　電流値
    2.2.3.TrainClass(int)　種別ID
    2.2.4.FromStation(string)　始発ID
    2.2.5.DestinatonStation(string)　行先ID
    2.2.5.Delay(int)　遅延分
*/
