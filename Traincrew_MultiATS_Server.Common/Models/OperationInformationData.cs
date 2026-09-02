namespace Traincrew_MultiATS_Server.Common.Models;

public class OperationInformationData
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public OperationInformationType Type { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
