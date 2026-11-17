using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TtcPathRoute;

public class TtcPathRouteRepository(ApplicationDbContext context) : ITtcPathRouteRepository
{
    public async Task<List<Models.TtcPathRoute>> GetAll(CancellationToken cancellationToken = default)
    {
        return await context.TtcPathRoutes
            .ToListAsync(cancellationToken);
    }
}
