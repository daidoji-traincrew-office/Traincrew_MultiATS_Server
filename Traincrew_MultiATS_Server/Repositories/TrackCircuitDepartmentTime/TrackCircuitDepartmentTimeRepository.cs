using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TrackCircuitDepartmentTime;

public class TrackCircuitDepartmentTimeRepository(ApplicationDbContext context)
    : ITrackCircuitDepartmentTimeRepository
{
    public async Task<Models.TrackCircuitDepartmentTime?> GetByTrackCircuitIdAndMaxCarCount(ulong trackCircuitId,
        int maxCarCount)
    {
        return await context.TrackCircuitDepartmentTimes
            .Where(tcdt => tcdt.TrackCircuitId == trackCircuitId && tcdt.CarCount <= maxCarCount)
            .OrderByDescending(tcdt => tcdt.CarCount)
            .FirstOrDefaultAsync();
    }
}
