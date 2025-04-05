using CsvHelper;
using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Models;

public class OperationNotificationDisplayCsv
{
    public required string Name { get; set; } // 告知器の名前 (Primary Key)
    public required string StationId { get; set; } // 所属する停車場
    public required List<string> TrackCircuitNames { get; set; } // 軌道回路名
    public required bool IsUp { get; set; } // 上り
    public required bool IsDown { get; set; } // 下り
}

public sealed class OperationNotificationDisplayCsvMap : ClassMap<OperationNotificationDisplayCsv>
{
    public OperationNotificationDisplayCsvMap()
    {
        Map(m => m.Name).Index(0);
        Map(m => m.StationId).Index(1);
        Map(m => m.TrackCircuitNames)
            .Convert(GetTrackCircuitNames);
        Map(m => m.IsUp)
            .Convert(row => IsFieldEqualToO(row, 5));
        Map(m => m.IsDown)
            .Convert(row => IsFieldEqualToO(row, 6));
    }

    private static List<string> GetTrackCircuitNames(ConvertFromStringArgs row)
    {
        IEnumerable<int> indices = [2, 3, 4];
        return indices.Select(i => row.Row.GetField(i))
            .OfType<string>()
            .Where(s => s != "なし")
            .ToList();
    }

    private static bool IsFieldEqualToO(ConvertFromStringArgs row, int index)
    {
        var value = row.Row.GetField(index);
        return value == "O";
    }
}