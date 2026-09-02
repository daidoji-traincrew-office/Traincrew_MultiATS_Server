using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TrackCircuitDepartmentTime;

public class TrackCircuitDepartmentTimeRepository(ApplicationDbContext context)
    : ITrackCircuitDepartmentTimeRepository
{
    public async Task<Models.TrackCircuitDepartmentTime?> GetByTrackCircuitIdAndIsUpAndMaxCarCount(
        ulong trackCircuitId,
        bool isUp,
        int maxCarCount)
    {
        return await context.TrackCircuitDepartmentTimes
            .Where(tcdt => tcdt.TrackCircuitId == trackCircuitId
                        && tcdt.IsUp == isUp
                        && tcdt.CarCount <= maxCarCount)
            .OrderByDescending(tcdt => tcdt.CarCount)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Models.TrackCircuitDepartmentTime>> GetByTrackCircuitIds(
        List<ulong> trackCircuitIds,
        CancellationToken cancellationToken = default)
    {
        return await context.TrackCircuitDepartmentTimes
            .Where(tcdt => trackCircuitIds.Contains(tcdt.TrackCircuitId))
            .ToListAsync(cancellationToken);
    }
}
