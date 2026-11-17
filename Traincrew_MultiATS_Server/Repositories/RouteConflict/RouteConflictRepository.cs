using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.RouteConflict;

public class RouteConflictRepository(ApplicationDbContext context) : IRouteConflictRepository
{
    public async Task<List<Models.RouteConflict>> GetAll(CancellationToken cancellationToken = default)
    {
        return await context.RouteConflicts
            .ToListAsync(cancellationToken);
    }
}
