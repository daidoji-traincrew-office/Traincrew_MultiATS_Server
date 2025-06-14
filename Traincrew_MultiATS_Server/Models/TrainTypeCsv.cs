using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Models;

public class TrainTypeCsv
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
}

public sealed class TrainTypeCsvMap : ClassMap<TrainTypeCsv>
{
    public TrainTypeCsvMap()
    {
        Map(m => m.Id).Name("種別id");
        Map(m => m.Name).Name("種別名");
    }
}
