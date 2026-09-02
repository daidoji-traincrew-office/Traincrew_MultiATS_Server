using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;

namespace Traincrew_MultiATS_Server.Services;

public interface ISwitchingMachineService
{
    Task<List<SwitchData>> GetAllSwitchData();
}

public class SwitchingMachineService(
    ISwitchingMachineRepository switchingMachineRepository
) : ISwitchingMachineService
{
    public static SwitchData ToSwitchData(SwitchingMachine switchingMachine)
    {
        var state = switchingMachine.SwitchingMachineState;

        return new()
        {
            Name = switchingMachine.Name,
            State = state.IsSwitching ? NRC.Center : state.IsReverse == NR.Normal ? NRC.Normal : NRC.Reversed
        };
    }

    public async Task<List<SwitchData>> GetAllSwitchData()
    {
        var switchingMachines = await switchingMachineRepository.GetSwitchingMachinesWithState();
        return switchingMachines.Select(ToSwitchData).ToList();
    }
}
