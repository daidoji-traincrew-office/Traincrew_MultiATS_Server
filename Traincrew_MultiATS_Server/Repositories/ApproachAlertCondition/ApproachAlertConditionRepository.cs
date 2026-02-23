using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.ApproachAlertCondition;

public class ApproachAlertConditionRepository(ApplicationDbContext context)
    : IApproachAlertConditionRepository
{
    public async Task<Models.ApproachAlertCondition> AddAndSaveAsync(
        Models.ApproachAlertCondition entity,
        CancellationToken cancellationToken = default)
    {
        context.ApproachAlertConditions.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAll(CancellationToken cancellationToken = default)
    {
        await context.ApproachAlertConditions.ExecuteDeleteAsync(cancellationToken);
    }
}
