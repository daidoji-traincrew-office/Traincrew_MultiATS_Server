using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.Diagram;

public class DiagramRepository(ApplicationDbContext context) : IDiagramRepository
{
    public async Task<Dictionary<(string Name, string TimeRange), Models.Diagram>> GetAllForNameAndTimeRange(
        CancellationToken cancellationToken = default)
    {
        return await context.Diagrams
            .ToDictionaryAsync(d => (d.Name, d.TimeRange), cancellationToken);
    }
}
