using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using TypedSignalR.Client;

namespace Traincrew_MultiATS_Server.LoadTest;


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

public abstract class TaskBase
{
    protected abstract int Delay { get; }
    protected abstract string HubPath { get; }
    private readonly List<CancellationTokenSource> _cancellationTokens = [];

    protected abstract Task ExecuteHubMethodInternal(HubConnection connection, int index, CancellationToken token);

    // タスクを実行するメソッド
    private async Task ExecuteTask(int index, CancellationToken token)
    {
        try
        {
            // Todo: ServerAddress.csから取得できるようにしたほうが良いかな
            var connection = new HubConnectionBuilder()
                .WithUrl($"https://localhost:7232/hub/{HubPath}")
                .WithAutomaticReconnect()
                .Build();

            await connection.StartAsync(token);
            while (true)
            {
                var delay = Task.Delay(Delay, token);
                await ExecuteHubMethodInternal(connection, index, token);
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
        var index = _cancellationTokens.Count;
        Task.Run(() => ExecuteTask(index, source.Token), CancellationToken.None);
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
    protected override int Delay => 100;
    protected override string HubPath => "train";
    private static readonly List<string> trackCircuits =
    [
        "TH76_5LAT",
        "TH76_5LBT",
        "TH76_5LCT",
        "TH76_5LDT",
        "TH75_5RAT",
        "TH75_5RBT",
        "TH75_9LCT",
        "TH71_1RAT",
        "TH71_1RBT",
        "TH71_6LCT",
        "TH71_6LDT",
        "TH70_1RAT",
        "TH70_5LBT",
        "TH67_1RAT",
        "TH67_1RBT",
        "TH67_4LT",
        "TH67_5LT",
        "TH65_2RT",
        "TH65_3RT",
        "TH65_11LT",
        "TH65_12LT",
        "TH64_12RT",
        "TH64_15LT",
        "TH63_12RT",
        "TH63_15LT",
        "TH62_12RT",
        "TH62_15LT",
        "TH61_2RAT",
        "TH61_2RBT",
        "TH61_6LT",
        "TH59_11RT",
        "TH59_13LT",
        "TH58_2RAT",
        "TH58_9LCT"
    ];

    protected override async Task ExecuteHubMethodInternal(HubConnection connection, int index, CancellationToken token)
    {
        var hub = connection.CreateHubProxy<ITrainHubContract>();
        var data = GenerateData(index);
        await hub.SendData_ATS(data);
    }

    private AtsToServerData GenerateData(int index)
    {
        var diaName = $"10{2 * index:D2}";
        while (true)
        {
            List<string> trackCircuit = [trackCircuits[index]];
            var returnData = new AtsToServerData
            {
                DiaName = diaName,
                OnTrackList = trackCircuit
                    .Select(tc => new Traincrew_MultiATS_Server.Common.Models.TrackCircuitData
                    {
                        Name = tc,
                        Last = "",
                        On = false,
                        Lock = false
                    })
                    .ToList(),
                BNotch = 0,
                PNotch = 0,
                Speed = 0,
                BougoState = false,
                CarStates = []
            };
            return returnData;
        }
    }
}

public class Signal : TaskBase
{
    public static Init_Data init_Data = new Init_Data();
    public static List<string> stationList = init_Data.Get_stationList();
    public static List<string> current_stationList = new List<string>();
    protected override int Delay => 100;
    protected override string HubPath => "interlocking";

    protected override async Task ExecuteHubMethodInternal(HubConnection connection, int index, CancellationToken token)
    {
        var hub = connection.CreateHubProxy<IInterlockingHubContract>();
        var activeStations = GenerateData();
        await hub.SendData_Interlocking(activeStations);
    }

    public List<string> GenerateData()
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
                current_stationList.Add(station);
                return new List<string> { station };
            }
        }
    }
}

public class TID : TaskBase
{
    protected override int Delay => 333;
    protected override string HubPath => "TID";

    protected override async Task ExecuteHubMethodInternal(HubConnection connection, int index, CancellationToken token)
    {
        var hub = connection.CreateHubProxy<ITIDHubContract>();
        await hub.SendData_TID();
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