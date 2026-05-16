using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Models;

public class DiagramTrainCsv
{
    public string TrainNumber { get; set; } = default!;
    public long TypeId { get; set; }
    public string FromStationId { get; set; } = default!;
    public string ToStationId { get; set; } = default!;
    public ulong DiaId { get; set; }
}

public sealed class DiagramTrainCsvMap : ClassMap<DiagramTrainCsv>
{
    public DiagramTrainCsvMap()
    {
        Map(m => m.TrainNumber).Name("列番");
        Map(m => m.TypeId).Name("種別id");
        Map(m => m.FromStationId).Name("始発駅id");
        Map(m => m.ToStationId).Name("行先駅id");
        Map(m => m.DiaId).Name("ダイヤid");
    }
}
