namespace Traincrew_MultiATS_Server.Models;

public class DiagramJson
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public List<TTC_Train> TrainList { get; set; } = [];
}
