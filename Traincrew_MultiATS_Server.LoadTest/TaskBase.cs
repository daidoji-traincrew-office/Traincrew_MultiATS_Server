using Microsoft.AspNetCore.SignalR.Client;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;

namespace Traincrew_MultiATS_Server.LoadTest;


public class ConstantDataFromInterlocking
{
    /// <summary>
    /// 常時送信用駅データリスト
    /// </summary>
    public List<string> ActiveStationsList { get; set; }
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

public class Station
{
    [Key]
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsStation { get; init; }
    public required bool IsPassengerStation { get; init; }
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


public abstract class TaskBase
{
    protected abstract int Delay { get; }
    protected abstract string Path { get; }
    protected abstract string Method { get; }
    protected abstract Object  Object { get; }
    private readonly List<CancellationTokenSource> _cancellationTokens = [];
    
    // タスクを実行するメソッド
    private async Task ExecuteTask(CancellationToken token)
    {
        try
        {
            // Todo: ServerAddress.csから取得できるようにしたほうが良いかな
            var connection = new HubConnectionBuilder()
            .WithUrl($"https://localhost:5154/hub/{Path}" )
            .WithAutomaticReconnect()
            .Build();
            await connection.StartAsync(token);
            while (true)
            {
                var delay = Task.Delay(Delay, token);
                await connection.InvokeAsync(Method,Object,token);
                await delay;
            }
        }
        catch (TaskCanceledException)
        {
            // タスクがキャンセルされた場合の処理
        }
        catch (Exception ex)
        {
            // その他の例外処理
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    // 現在のカウントを返すメソッド
    public int GetCurrentCount()
    {
        return _cancellationTokens.Count;
    }

    // 現在の値を増やし、新しいタスクを実行するメソッド
    public void IncrementAndExecute()
    {
        var source = new CancellationTokenSource();
        Task.Run(() => ExecuteTask(source.Token), CancellationToken.None);
        _cancellationTokens.Add(source);
    }

    // 3. 現在の値を減らし、現在実行中のタスクを1つキャンセルするメソッド
    public void DecrementAndCancel()
    {
        if (GetCurrentCount() <= 0) return;
        _cancellationTokens.Last().Cancel();
        _cancellationTokens.RemoveAt(_cancellationTokens.Count - 1);
    }
}

public class ATS : TaskBase
{
    public static Init_Data init_Data = new Init_Data();
    public static List<TrackCircuitData> DB_TrackCircuitDataList = init_Data.Get_TrackCuircuitDataList();
    public static List<TrackCircuitData> Current_TrackCircuitDataList = new List<TrackCircuitData>();
    protected override int Delay => 100;
    protected override string Path => "train";
    protected override string Method => "SendData_ATS";
    protected override object Object => GenerateData();

    public async Task<DataToServer> GenerateData()
    {
        bool flag = false;
        Random random = new Random();
        string diaName = random.Next(1, 100).ToString();
        while (true)
        {
            DataToServer returnData = new DataToServer()
            {
                DiaName = diaName,
                OnTrackList = new List<TrackCircuitData>
                {
                    new TrackCircuitData{Name = DB_TrackCircuitDataList[random.Next(DB_TrackCircuitDataList.Count())].Name, Last = diaName, On = true}
                },
            };
            foreach (var item in Current_TrackCircuitDataList)
            {
                if (item.Name == returnData.OnTrackList.First().Name)
                    flag = true;
            }
            if (!flag)
            {
                returnData.OnTrackList.ForEach(Current_TrackCircuitDataList.Add);
                return returnData;
            }
        }
    }
}

public class Signal : TaskBase
{
    public static Init_Data init_Data = new Init_Data();
    public static List<string> stationList = init_Data.Get_stationList();
    public static List<string> current_stationList = new List<string>();
    protected override int Delay => 100;
    protected override string Path => "interlocking";
    protected override string Method => "SendData_Interlocking";
    protected override object Object => GenerateData();
    public ConstantDataFromInterlocking GenerateData()
    {
        Random random = new Random();
        while (true)
        {
            bool flag = false;
            string station = stationList[random.Next(stationList.Count)];
            foreach (var item in current_stationList)
            {
                if (station == item)
                    flag = true;
            }
            if (!flag)
            {
                ConstantDataFromInterlocking constantData = new ConstantDataFromInterlocking()
                {
                    ActiveStationsList = new List<string>() { stationList[random.Next(stationList.Count)] }
                };
                return constantData;
            }
        }

    }
}

public class TID : TaskBase
{
    protected override int Delay => 333;
    protected override string Path => "TID";
    protected override string Method => "SendData_TID";
    protected override object Object => GenerateData();
    public Object GenerateData()
    {
        return new Object();
    }
}

public class Init_Data
{
    public List<TrackCircuitData> Get_TrackCuircuitDataList()
    {
        //相対パスに修正しておく
        var jsonstring = File.ReadAllText("G:\\prog\\Traincrew_MultiATS_Server\\Traincrew_MultiATS_Server\\Data\\DBBase.json");
        var DBBase = JsonSerializer.Deserialize<DBBasejson>(jsonstring);
        List<TrackCircuitData> returnData = new List<TrackCircuitData>();
        foreach (var item in DBBase.trackCircuitList)
        {
            returnData.Add(new TrackCircuitData { Name = item.Name});
        }
        return returnData;
    }

    public List<string> Get_stationList()
    {
        //相対パスに修正しておく
        var jsonstring = File.ReadAllText("G:\\prog\\Traincrew_MultiATS_Server\\Traincrew_MultiATS_Server\\Data\\DBBase.json");
        var DBBase = JsonSerializer.Deserialize<DBBasejson>(jsonstring);
        List<string> returnData = DBBase.stationList.Select(x => x.Name).ToList();
        return returnData;
    }
}