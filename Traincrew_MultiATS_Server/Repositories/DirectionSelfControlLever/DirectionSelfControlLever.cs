using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.DirectionSelfControlLever;

public class DirectionSelfControlLeverRepository(ApplicationDbContext context) : IDirectionSelfControlLeverRepository
{
    /// <summary>
    /// 開放てこを名前から取得する。
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public async Task<Models.DirectionSelfControlLever?> GetDirectionSelfControlLeverByNameWithState(string name)
    {
        return await context.DirectionSelfControlLevers
            .Include(lever => lever.DirectionSelfControlLeverState)
            .FirstOrDefaultAsync(lever => lever.Name == name);
    }
}