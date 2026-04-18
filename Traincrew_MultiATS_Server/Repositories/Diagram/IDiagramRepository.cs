namespace Traincrew_MultiATS_Server.Repositories.Diagram;

public interface IDiagramRepository
{
    Task<Dictionary<(string Name, string TimeRange), Models.Diagram>> GetAllForNameAndTimeRange(
        CancellationToken cancellationToken = default);
}
