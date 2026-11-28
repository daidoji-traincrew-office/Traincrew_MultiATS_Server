using CsvHelper;
using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Models;

public class TtcWindowCsv
{
    public required string Name { get; set; } // 窓名
    public required string StationId { get; set; } // 所属駅
    public string TrackNumber { get; set; } = ""; // 番線(空欄・なしは空文字)
    public List<string> DisplayStations { get; set; } = []; // 表示駅リスト
    public TtcWindowType Type { get; set; } // 種類
    public List<string> TrackCircuits { get; set; } = []; // 対応軌道回路リスト
}

public sealed class TtcWindowCsvMap : ClassMap<TtcWindowCsv>
{
    public TtcWindowCsvMap()
    {
        Map(m => m.Name).Index(0);
        Map(m => m.StationId).Index(1);
        Map(m => m.TrackNumber).Convert(GetTrackNumber);
        Map(m => m.DisplayStations).Convert(GetDisplayStations);
        Map(m => m.Type).Convert(GetType);
        Map(m => m.TrackCircuits).Convert(GetTrackCircuits);
    }
    
    private static string GetTrackNumber(ConvertFromStringArgs row)
    {
        var value = row.Row.GetField(2);
        return string.IsNullOrWhiteSpace(value) || value == "なし" ? "" : value;
    }

    private static TtcWindowType GetType(ConvertFromStringArgs row)
    {
        var value = row.Row.GetField(7);
        return value switch
        {
            "上り閉塞" => TtcWindowType.Up,
            "下り閉塞" => TtcWindowType.Down,
            "ホーム番線" or "場内途中" or "出発途中" => TtcWindowType.HomeTrack,
            "入換番線" => TtcWindowType.ShuntTrack,
            "本線入換" => TtcWindowType.Switching,
            _ => throw new InvalidOperationException($"Invalid TtcWindowType value: {value}")
        };
    }
    
    private static List<string> GetDisplayStations(ConvertFromStringArgs row)
    {
        IEnumerable<int> indices = [3, 4, 5, 6];
        return indices.Select(i => row.Row.GetField(i))
            .OfType<string>()
            .Where(s => !string.IsNullOrWhiteSpace(s) && s != "なし")
            .ToList();
    }

    private static List<string> GetTrackCircuits(ConvertFromStringArgs row)
    {
        IEnumerable<int> indices = [8, 9];
        return indices.Select(i => row.Row.GetField(i))
            .OfType<string>()
            .Where(s => !string.IsNullOrWhiteSpace(s) && s != "なし")
            .ToList();
    }
    
}