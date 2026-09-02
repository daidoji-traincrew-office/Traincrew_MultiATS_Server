using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.Diagram;

public class DiagramRepository(ApplicationDbContext context) : IDiagramRepository
{
    public async Task<Dictionary<string, Models.Diagram>> GetAllForName(
        CancellationToken cancellationToken = default)
    {
        return await context.Diagrams
            .ToDictionaryAsync(d => d.Name, cancellationToken);
    }

    public async Task<List<Models.Diagram>> GetAll(CancellationToken cancellationToken = default)
    {
        return await context.Diagrams.ToListAsync(cancellationToken);
    }
}
