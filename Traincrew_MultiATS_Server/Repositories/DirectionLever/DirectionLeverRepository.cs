using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.DirectionLever;

public class DirectionLeverRepository(ApplicationDbContext context) : IDirectionLeverRepository
{
    public async Task<List<ulong>> GetAllIds() 
    {
        return await context.DirectionLevers
            .Select(dl => dl.Id)
            .ToListAsync();
    }
}
