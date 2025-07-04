using CsvHelper;
using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Models;

public class TtcWindowCsv
{
    public required string Name { get; set; } // 窓名
    public required string StationId { get; set; } // 所属駅
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
        Map(m => m.DisplayStations).Convert(GetDisplayStations);
        Map(m => m.Type).Convert(GetType);
        Map(m => m.TrackCircuits).Convert(GetTrackCircuits);
    }
    
    private static TtcWindowType GetType(ConvertFromStringArgs row)
    {
        var value = row.Row.GetField(6);
        return value switch
        {
            "上り" => TtcWindowType.Up,
            "下り" => TtcWindowType.Down,
            "番線" => TtcWindowType.HomeTrack,
            "入換" => TtcWindowType.Switching,
            _ => throw new InvalidOperationException($"Invalid TtcWindowType value: {value}")
        };
    }
    
    private static List<string> GetDisplayStations(ConvertFromStringArgs row)
    {
        IEnumerable<int> indices = [2, 3, 4, 5];
        return indices.Select(i => row.Row.GetField(i))
            .OfType<string>()
            .Where(s => !string.IsNullOrWhiteSpace(s) && s != "なし")
            .ToList();
    }

    private static List<string> GetTrackCircuits(ConvertFromStringArgs row)
    {
        IEnumerable<int> indices = [7, 8];
        return indices.Select(i => row.Row.GetField(i))
            .OfType<string>()
            .Where(s => !string.IsNullOrWhiteSpace(s) && s != "なし")
            .ToList();
    }
    
}