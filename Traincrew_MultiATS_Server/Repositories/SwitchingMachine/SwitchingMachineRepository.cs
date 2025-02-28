using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.SwitchingMachine;

public class SwitchingMachineRepository(ApplicationDbContext context) : ISwitchingMachineRepository
{
    public Task<List<Models.SwitchingMachine>> GetSwitchingMachinesWithState()
    {
        return context.SwitchingMachines.Include(sm => sm.SwitchingMachineState).ToListAsync();
    }

    public async Task<List<Models.SwitchingMachine>> GetSwitchingMachinesByIdsWithState(IEnumerable<ulong> ids)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateSwitchEndTime(IEnumerable<ulong> ids, DateTime switchEndTime)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateIsReverse(IEnumerable<ulong> ids, bool isReverse)
    {
        throw new NotImplementedException();
    }
}