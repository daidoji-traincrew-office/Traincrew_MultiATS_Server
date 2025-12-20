using CsvHelper.Configuration;

namespace Traincrew_MultiATS_Server.Models;

public class SignalTypeCsv
{
    public required string Name { get; set; } // 名前
    public required string RIndication { get; set; } // 次がR
    public required string YYIndication { get; set; } // 次がYY
    public required string YIndication { get; set; } // 次がY
    public required string YGIndication { get; set; } // 次がYG
    public required string GIndication { get; set; } // 次がG
}

public sealed class SignalTypeCsvMap : ClassMap<SignalTypeCsv>
{
    public SignalTypeCsvMap()
    {
        Map(m => m.Name).Name("名前");
        Map(m => m.RIndication).Name("次がR");
        Map(m => m.YYIndication).Name("次がYY");
        Map(m => m.YIndication).Name("次がY");
        Map(m => m.YGIndication).Name("次がYG");
        Map(m => m.GIndication).Name("次がG");
    }
}
