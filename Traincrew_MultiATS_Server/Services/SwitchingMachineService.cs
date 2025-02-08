using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachine;

namespace Traincrew_MultiATS_Server.Services;

public class SwitchingMachineService(
    DateTimeRepository dateTimeRepository,
    SwitchingMachineRepository switchingMachineRepository)
{
    private const int SwitchingTime = 3;
    public async Task SetSwitchingMachines(List<(ulong, NR)> switchingMachineList)
    {
        var now = dateTimeRepository.GetNow();
        var switchingMachineIds = switchingMachineList.Select(x => x.Item1);
        var switchingMachines = await switchingMachineRepository
            .GetSwitchingMachinesWithState(switchingMachineIds);
        var switchingMachineDic = switchingMachines.ToDictionary(x => x.Id);
        // Todo: 鎖状状態の取得と確認
        
        foreach (var (id, isReverse) in switchingMachineList)
        {
            if (!switchingMachineDic.TryGetValue(id, out var switchingMachine))
            {
                // Todo: 存在しないIDなので例外
                continue;
            }
            if (switchingMachine.SwitchingMachineState.SwitchEndTime > now)
            {
                // Todo: 転換中なので例外
            }
            /*
            if (switchingMachine.SwitchingMachineState.IsReverse == isReverse)
            {
                continue;
            }
            */
        }
    }
}