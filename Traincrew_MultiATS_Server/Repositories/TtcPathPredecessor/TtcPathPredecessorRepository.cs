using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TtcPathPredecessor;

public class TtcPathPredecessorRepository(ApplicationDbContext context) : ITtcPathPredecessorRepository
{
    public async Task<List<Models.TtcPathPredecessor>> GetAll(CancellationToken cancellationToken = default)
    {
        return await context.TtcPathPredecessors
            .ToListAsync(cancellationToken);
    }
}
