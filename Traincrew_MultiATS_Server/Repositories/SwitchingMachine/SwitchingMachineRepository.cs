namespace Traincrew_MultiATS_Server.Repositories.SwitchingMachine;

public class SwitchingMachineRepository : ISwitchingMachineRepository
{
    public async Task<List<Models.SwitchingMachine>> GetSwitchingMachinesWithState(IEnumerable<ulong> ids)
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