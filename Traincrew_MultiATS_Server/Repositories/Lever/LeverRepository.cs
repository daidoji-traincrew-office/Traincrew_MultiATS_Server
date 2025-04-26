using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.Lever;

public class LeverRepository(ApplicationDbContext context) : ILeverRepository
{
    public async Task<Models.Lever?> GetLeverByNameWitState(string name)
    {
        return await context.Levers
            .Include(lever => lever.LeverState)
            .FirstOrDefaultAsync(lever => lever.Name == name);
    }
    
    public async Task<List<ulong>> GetAllDirectionLeverIds()
    {
        return await context.DirectionRoutes
            .Select(dl => dl.LeverId)
            .ToListAsync();
    }
}