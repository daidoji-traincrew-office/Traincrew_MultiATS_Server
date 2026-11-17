using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TtcPath;

public class TtcPathRepository(ApplicationDbContext context) : ITtcPathRepository
{
    public async Task<List<Models.TtcPath>> GetAll(CancellationToken cancellationToken = default)
    {
        return await context.TtcPaths
            .ToListAsync(cancellationToken);
    }
}
