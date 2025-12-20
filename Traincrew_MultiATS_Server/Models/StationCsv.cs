using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Models;

public class StationCsv
{
    public required string Id { get; set; } // 駅ID
    public required string Name { get; set; } // 駅名
    public required bool IsStation { get; set; } // 停車場?
    public required bool IsPassengerStation { get; set; } // 旅客駅?
}

public sealed class StationCsvMap : ClassMap<StationCsv>
{
    public StationCsvMap()
    {
        Map(m => m.Id).Name("駅ID");
        Map(m => m.Name).Name("駅名");
        Map(m => m.IsStation).Convert(row => ParseOX(row.Row.GetField("停車場？")));
        Map(m => m.IsPassengerStation).Convert(row => ParseOX(row.Row.GetField("旅客駅？")));
    }

    private static bool ParseOX(string? value)
    {
        return value == "O";
    }
}
