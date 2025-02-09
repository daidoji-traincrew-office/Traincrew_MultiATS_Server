namespace Traincrew_MultiATS_Server.Repositories.SwitchingMachine;
using Datetime = DateTime;

public interface ISwitchingMachineRepository
{
    Task<List<Models.SwitchingMachine>> GetSwitchingMachinesWithState();
    Task<List<Models.SwitchingMachine>> GetSwitchingMachinesByIdsWithState(IEnumerable<ulong> ids);
    Task UpdateSwitchEndTime(IEnumerable<ulong> ids, Datetime switchEndTime);
    Task UpdateIsReverse(IEnumerable<ulong> ids, bool isReverse);
}