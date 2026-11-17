using CsvHelper;
using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Models;

public class TtcPathCsv
{
    public required string Name { get; set; }
    public required string Station { get; set; }
    public required string SourceTrack { get; set; }
    public required string TargetTrack { get; set; }
    public required TtcPathType Type { get; set; }
    public required List<string> TargetRoutes { get; set; }
    public required int DefineOrderTrackCircuitCount { get; set; }
    public required List<string> ApproachTrackCircuits { get; set; }
    public required List<string> Predecessor { get; set; }
}

public sealed class TtcPathCsvMap : ClassMap<TtcPathCsv>
{
    private static readonly string[] TargetRouteFields = ["対応進路①", "対応進路②", "対応進路③"];

    private static readonly string[] ApproachTrackCircuitFields =
        [.. Enumerable.Range(1, 15).Select(i => $"接近判定{i}")];

    private static readonly string[] PredecessorFields =
        ["近接前対応①", "近接前対応②", "近接前対応③", "近接前対応④", "近接前対応⑤"];

    public TtcPathCsvMap()
    {
        Map(m => m.Name).Name("名称");
        Map(m => m.Station).Name("対象駅");
        Map(m => m.SourceTrack).Name("元番線");
        Map(m => m.TargetTrack).Name("先番線");
        Map(m => m.Type).Convert(GetType);
        Map(m => m.TargetRoutes).Convert(row => GetList(row, TargetRouteFields));
        Map(m => m.DefineOrderTrackCircuitCount).Name("順序断定回路数");
        Map(m => m.ApproachTrackCircuits).Convert(row => GetList(row, ApproachTrackCircuitFields));
        Map(m => m.Predecessor).Convert(row => GetList(row, PredecessorFields));
    }
    
    private static TtcPathType GetType(ConvertFromStringArgs row)
    {
        var value = row.Row.GetField("種類");
        return value switch
        {
            "場内" => TtcPathType.Arriving,
            "出発" => TtcPathType.Departure,
            "入換" => TtcPathType.Switching,
            _ => throw new InvalidOperationException($"Invalid path type: {value}")
        };
    }

    private static List<string> GetList(ConvertFromStringArgs row, IEnumerable<string> fieldNames)
    {
        return fieldNames
            .Select(name => row.Row.GetField(name))
            .OfType<string>()
            .Where(s => !string.IsNullOrWhiteSpace(s) && s != "なし")
            .ToList();
    }
}
