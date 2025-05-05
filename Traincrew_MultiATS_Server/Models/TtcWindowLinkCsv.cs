using CsvHelper;
using CsvHelper.Configuration;
using Traincrew_MultiATS_Server.Models.Enums;

namespace Traincrew_MultiATS_Server.Models;

public class TtcWindowLinkCsv
{
    public required string Source { get; set; } // 移動元
    public required string Target { get; set; } // 移動先
    public TtcWindowLinkType Type { get; set; } // 上下線
    public bool IsEmptySending { get; set; } // 空送り
    public string? TrackCircuitCondition { get; set; } // 移行条件軌道回路名
    public List<string> RouteConditions { get; set; } = []; // 移行条件進路名リスト
}

public sealed class TtcWindowLinkCsvMap : ClassMap<TtcWindowLinkCsv>
{
    public TtcWindowLinkCsvMap()
    {
        Map(m => m.Source).Index(0);
        Map(m => m.Target).Index(1);
        Map(m => m.Type).Convert(row => Enum.Parse<TtcWindowLinkType>(row.Row.GetField(2), true));
        Map(m => m.IsEmptySending).Convert(row => row.Row.GetField(3) == "O");
        Map(m => m.TrackCircuitCondition).Convert(getTrackCircuitCondition);
        Map(m => m.RouteConditions).Convert(GetRouteConditions);
    }
    
    private static List<string> GetRouteConditions(ConvertFromStringArgs row)
    {
        IEnumerable<int> indices = [5, 6, 7, 8, 9];
        return indices.Select(i => row.Row.GetField(i))
            .OfType<string>()
            .Where(s => !string.IsNullOrWhiteSpace(s) && s != "なし")
            .ToList();
    }
    
    private static string? getTrackCircuitCondition(ConvertFromStringArgs row)
    {
        var value = row.Row.GetField(4);
        return string.IsNullOrWhiteSpace(value) || value == "なし" ? null : value;
    }
}