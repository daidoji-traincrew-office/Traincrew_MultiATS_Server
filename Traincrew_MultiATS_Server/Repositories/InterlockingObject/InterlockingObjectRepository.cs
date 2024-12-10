using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.InterlockingObject;

public class InterlockingObjectRepository(ApplicationDbContext context): IInterlockingObjectRepository
{
    public Task GetObjectByIds(IEnumerable<ulong> ids)
    {
        // Todo: 渡されたIDのオブジェクトが、かならず存在することを保証する
        return context.InterlockingObjects
            .Where(obj => ids.Contains(obj.Id))
            .ToListAsync();
    }
    public Task<Models.InterlockingObject> GetObject(string stationId, string name)
    {
        return context.InterlockingObjects
            .Where(obj => obj.StationId == stationId && obj.Name == name)
            .FirstAsync();
    }
}