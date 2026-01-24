using CsvHelper;
using CsvHelper.Configuration;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;

namespace Traincrew_MultiATS_Server.Models;

public class TrackCircuitCsv
{
    public required string Name { get; set; } // 軌道回路DB名
    public required List<string> NextSignalNamesUp { get; set; } // 上り信号機1-5
    public required List<string> NextSignalNamesDown { get; set; } // 下り信号機1-5
    public int? ProtectionZone { get; set; } // 防護無線ゾーン
    public string? TargetStation { get; set; } // 対象駅(列32)
    public int? UpTimeElement6Car { get; set; } // 上り進出時素6連(列33)
    public int? UpTimeElement4Car { get; set; } // 上り進出時素4連(列34)
    public int? UpTimeElement2Car { get; set; } // 上り進出時素2連(列35)
    public int? UpTimeElementPass { get; set; } // 上り進出時素通過(列36)
    public int? DownTimeElement6Car { get; set; } // 下り進出時素6連(列37)
    public int? DownTimeElement4Car { get; set; } // 下り進出時素4連(列38)
    public int? DownTimeElement2Car { get; set; } // 下り進出時素2連(列39)
    public int? DownTimeElementPass { get; set; } // 下り進出時素通過(列40)
}

public sealed class TrackCircuitCsvMap : ClassMap<TrackCircuitCsv>
{
    public TrackCircuitCsvMap()
    {
        Map(m => m.Name).Name("軌道回路DB名");
        Map(m => m.NextSignalNamesUp).Convert(GetNextSignalNamesUp);
        Map(m => m.NextSignalNamesDown).Convert(GetNextSignalNamesDown);
        Map(m => m.ProtectionZone).Convert(GetProtectionZone);

        // 列32: 対象駅
        Map(m => m.TargetStation).Index(32).TypeConverter<EmptyStringToNullConverter>();

        // 列33-40: 時素値
        Map(m => m.UpTimeElement6Car).Index(33).TypeConverter<EmptyStringToNullableIntConverter>();
        Map(m => m.UpTimeElement4Car).Index(34).TypeConverter<EmptyStringToNullableIntConverter>();
        Map(m => m.UpTimeElement2Car).Index(35).TypeConverter<EmptyStringToNullableIntConverter>();
        Map(m => m.UpTimeElementPass).Index(36).TypeConverter<EmptyStringToNullableIntConverter>();
        Map(m => m.DownTimeElement6Car).Index(37).TypeConverter<EmptyStringToNullableIntConverter>();
        Map(m => m.DownTimeElement4Car).Index(38).TypeConverter<EmptyStringToNullableIntConverter>();
        Map(m => m.DownTimeElement2Car).Index(39).TypeConverter<EmptyStringToNullableIntConverter>();
        Map(m => m.DownTimeElementPass).Index(40).TypeConverter<EmptyStringToNullableIntConverter>();
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
