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

    public async Task AddAsync(Models.TrainType trainType, CancellationToken cancellationToken = default)
    {
        await context.TrainTypes.AddAsync(trainType, cancellationToken);
    }
}
