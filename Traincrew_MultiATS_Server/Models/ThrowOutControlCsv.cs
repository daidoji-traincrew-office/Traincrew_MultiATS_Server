using CsvHelper;
using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Models;

public class ThrowOutControlCsv
{
    public required string SourceLever { get; set; } // 総括制御元
    public required string TargetLever { get; set; } // 総括制御先
    public string? LeverCondition { get; set; } // てこ条件(方向の場合)
    public ThrowOutControlType Type { get; set; } // 総括種類
}

public sealed class ThrowOutControlCsvMap : ClassMap<ThrowOutControlCsv>
{
    public ThrowOutControlCsvMap()
    {
        Map(m => m.SourceLever).Index(0);
        Map(m => m.TargetLever).Index(1);
        Map(m => m.LeverCondition).Index(2).Optional();
        Map(m => m.Type).Convert(GetType);
    }

    private static ThrowOutControlType GetType(ConvertFromStringArgs row)
    {
        var value = row.Row.GetField(3);
        return value switch
        {
            "てこあり" => ThrowOutControlType.WithLever,
            "てこナシ" => ThrowOutControlType.WithoutLever,
            "方向" => ThrowOutControlType.Direction,
            _ => throw new InvalidOperationException($"Invalid ThrowOutControlType value: {value}")
        };
    }
}