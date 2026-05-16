using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TrainType;

public class TrainTypeRepository(ApplicationDbContext context) : ITrainTypeRepository
{
    public async Task<List<long>> GetIdsForAll(CancellationToken cancellationToken = default)
    {
        return await context.TrainTypes
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, long>> GetAllIdForName(CancellationToken cancellationToken = default)
    {
        return await context.TrainTypes
            .ToDictionaryAsync(t => t.Name, t => t.Id, cancellationToken);
    }
}
