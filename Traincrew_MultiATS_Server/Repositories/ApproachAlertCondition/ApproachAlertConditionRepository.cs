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

    public async Task<List<Models.ApproachAlertCondition>> GetByStationIdAndIsUpPairs(
        List<(string StationId, bool IsUp)> pairs)
    {
        var stationIds = pairs.Select(p => p.StationId).Distinct().ToList();
        var conditions = await context.ApproachAlertConditions
            .Include(c => c.TrackCircuit)
            .ThenInclude(tc => tc!.TrackCircuitState)
            .Where(c => stationIds.Contains(c.StationId))
            .ToListAsync();
        return conditions
            .Where(c => pairs.Any(p => p.StationId == c.StationId && p.IsUp == c.IsUp))
            .ToList();
    }
}
