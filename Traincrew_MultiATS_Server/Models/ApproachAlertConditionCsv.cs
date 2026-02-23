using CsvHelper.Configuration.Attributes;

namespace Traincrew_MultiATS_Server.Models;

public class ApproachAlertConditionCsv
{
    [Index(0)] public string StationName { get; set; } = "";
    [Index(1)] public string Note { get; set; } = "";
    [Index(2)] public string UpCondition { get; set; } = "";
    [Index(3)] public string DownCondition { get; set; } = "";
}
