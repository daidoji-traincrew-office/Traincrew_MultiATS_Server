using CsvHelper;
using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Models;

public class TrackCircuitCsv
{
    public required string Name { get; set; } // 軌道回路DB名
    public required List<string> NextSignalNamesUp { get; set; } // 上り信号機1-5
    public required List<string> NextSignalNamesDown { get; set; } // 下り信号機1-5
    public int? ProtectionZone { get; set; } // 防護無線ゾーン
}

public sealed class TrackCircuitCsvMap : ClassMap<TrackCircuitCsv>
{
    public TrackCircuitCsvMap()
    {
        Map(m => m.Name).Name("軌道回路DB名");
        Map(m => m.NextSignalNamesUp).Convert(GetNextSignalNamesUp);
        Map(m => m.NextSignalNamesDown).Convert(GetNextSignalNamesDown);
        Map(m => m.ProtectionZone).Convert(GetProtectionZone);
    }

    private static List<string> GetNextSignalNamesUp(ConvertFromStringArgs row)
    {
        var fieldNames = new[] { "上り信号機1", "上り信号機2", "上り信号機3", "上り信号機4", "上り信号機5" };
        return fieldNames
            .Select(name => row.Row.GetField(name))
            .OfType<string>()
            .Where(s => !string.IsNullOrWhiteSpace(s) && s != "なし")
            .ToList();
    }

    private static List<string> GetNextSignalNamesDown(ConvertFromStringArgs row)
    {
        var fieldNames = new[] { "下り信号機1", "下り信号機2", "下り信号機3", "下り信号機4", "下り信号機5" };
        return fieldNames
            .Select(name => row.Row.GetField(name))
            .OfType<string>()
            .Where(s => !string.IsNullOrWhiteSpace(s) && s != "なし")
            .ToList();
    }

    private static int? GetProtectionZone(ConvertFromStringArgs row)
    {
        var value = row.Row.GetField("防護無線ゾーン");
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return int.TryParse(value, out var result) ? result : null;
    }
}
