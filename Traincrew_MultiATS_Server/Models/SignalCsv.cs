using CsvHelper;
using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Models;

public class SignalCsv
{
    public required string Name { get; set; } // DB名
    public required bool IsImplemented { get; set; } // 実装済み区間
    public required string TypeName { get; set; } // 何灯式
    public required List<string> NextSignalNames { get; set; } // 次の信号機のDB名1-5
    public required List<string> RouteNames { get; set; } // 対応進路名1-11
    public string? DirectionRouteLeft { get; set; } // 対応方向進路名1
    public string? DirectionRouteRight { get; set; } // 対応方向進路名2
    public string? Direction { get; set; } // 向き
    public string? TrackCircuitName { get; set; } // 防護区間軌道回路
}

public sealed class SignalCsvMap : ClassMap<SignalCsv>
{
    public SignalCsvMap()
    {
        Map(m => m.Name).Name("DB名");
        Map(m => m.IsImplemented).Convert(row => IsFieldEqualToO(row, "実装済み区間"));
        Map(m => m.TypeName).Name("何灯式");
        Map(m => m.NextSignalNames).Convert(GetNextSignalNames);
        Map(m => m.RouteNames).Convert(GetRouteNames);
        Map(m => m.DirectionRouteLeft).Convert(row => GetNullableField(row, "対応方向進路名1"));
        Map(m => m.DirectionRouteRight).Convert(row => GetNullableField(row, "対応方向進路名2"));
        Map(m => m.Direction).Convert(row => GetNullableField(row, "向き"));
        Map(m => m.TrackCircuitName).Convert(row => GetNullableField(row, "防護区間軌道回路"));
    }

    private static List<string> GetNextSignalNames(ConvertFromStringArgs row)
    {
        var fieldNames = new[] { "次の信号機のDB名1", "次の信号機のDB名2", "次の信号機のDB名3", "次の信号機のDB名4", "次の信号機のDB名5" };
        return fieldNames
            .Select(name => row.Row.GetField(name))
            .OfType<string>()
            .Where(s => !string.IsNullOrWhiteSpace(s) && s != "なし")
            .ToList();
    }

    private static List<string> GetRouteNames(ConvertFromStringArgs row)
    {
        var fieldNames = new[] {
            "対応進路名1", "対応進路名2", "対応進路名3", "対応進路名4", "対応進路名5",
            "対応進路名6", "対応進路名7", "対応進路名8", "対応進路名9", "対応進路名10", "対応進路名11"
        };
        return fieldNames
            .Select(name => row.Row.GetField(name))
            .OfType<string>()
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static string? GetNullableField(ConvertFromStringArgs row, string fieldName)
    {
        var value = row.Row.GetField(fieldName);
        return !string.IsNullOrWhiteSpace(value) && value != "なし" ? value : null;
    }

    private static bool IsFieldEqualToO(ConvertFromStringArgs row, string fieldName)
    {
        var value = row.Row.GetField(fieldName);
        return value == "O";
    }
}
