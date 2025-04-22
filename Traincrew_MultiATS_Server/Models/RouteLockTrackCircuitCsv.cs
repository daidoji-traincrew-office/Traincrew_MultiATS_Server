using CsvHelper;
using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Models;

public class RouteLockTrackCircuitCsv
{
    public required string RouteName { get; set; } // 進路名
    public required List<string> TrackCircuitNames { get; set; } // 軌道回路名
}

public sealed class RouteLockTrackCircuitCsvMap : ClassMap<RouteLockTrackCircuitCsv>
{
    public RouteLockTrackCircuitCsvMap()
    {
        Map(m => m.RouteName).Index(0);
        Map(m => m.TrackCircuitNames)
            .Convert(GetTrackCircuitNames);
    }

    private static List<string> GetTrackCircuitNames(ConvertFromStringArgs row)
    {
        return (row.Row.Parser.Record ?? [])
            .Skip(1)
            .Where(s => s != "なし")
            .ToList();
    }
}