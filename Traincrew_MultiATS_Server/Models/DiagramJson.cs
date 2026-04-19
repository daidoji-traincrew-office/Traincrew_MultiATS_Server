using System.Text.Json.Serialization;

namespace Traincrew_MultiATS_Server.Models;

public class DiagramJson
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("time_range")]
    public string TimeRange { get; set; } = string.Empty;

    public int Index { get; set; }

    public List<TTC_Train> TrainList { get; set; } = [];
}
