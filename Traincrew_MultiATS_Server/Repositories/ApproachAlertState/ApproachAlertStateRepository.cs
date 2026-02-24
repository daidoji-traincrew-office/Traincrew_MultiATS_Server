using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.ApproachAlertState;

public class ApproachAlertStateRepository(ApplicationDbContext context) : IApproachAlertStateRepository
{
    public async Task<List<ulong>> GetIdsWhereShouldRing()
    {
        return await context.ApproachAlertStates
            .Where(s => s.ShouldRing)
            .Select(s => s.Id)
            .ToListAsync();
    }

    public async Task<List<ulong>> GetIdsWhereHasShortCircuitedCondition()
    {
        return await context.ApproachAlertStates
            .Where(s => context.ApproachAlertConditions
                .Any(c => c.StationId == s.StationId
                          && c.IsUp == s.IsUp
                          && c.TrackCircuit!.TrackCircuitState.IsShortCircuit))
            .Select(s => s.Id)
            .ToListAsync();
    }

    public async Task<List<Models.ApproachAlertState>> GetByIds(List<ulong> ids)
    {
        return await context.ApproachAlertStates
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();
    }

    public async Task SetIsRingingFalseByStationIdAndIsUp(string stationId, bool isUp)
    {
        await context.ApproachAlertStates
            .Where(s => s.StationId == stationId && s.IsUp == isUp)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRinging, false));
    }

    public async Task<List<Models.ApproachAlertState>> GetWhereIsRinging()
    {
        return await context.ApproachAlertStates
            .Where(s => s.IsRinging)
            .ToListAsync();
    }
}
