namespace Traincrew_MultiATS_Server.Repositories.Diagram;

public interface IDiagramRepository
{
    Task<Dictionary<string, Models.Diagram>> GetAllForName(
        CancellationToken cancellationToken = default);
    Task<List<Models.Diagram>> GetAll(CancellationToken cancellationToken = default);
}
