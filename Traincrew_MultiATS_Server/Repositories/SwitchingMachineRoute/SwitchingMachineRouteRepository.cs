using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;

public class SwitchingMachineRouteRepository(ApplicationDbContext context) : ISwitchingMachineRouteRepository
{
    public Task<List<Models.SwitchingMachineRoute>> GetAll()
    {
        return context.SwitchingMachineRoutes.ToListAsync();
    }
}