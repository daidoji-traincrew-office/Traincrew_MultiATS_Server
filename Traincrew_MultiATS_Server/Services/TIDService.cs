using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Services;

public class TIDService(
    TrackCircuitService trackCircuitService,
    SwitchingMachineService switchingMachineService,
    DirectionRouteService directionRouteService)
{
    public async Task<ConstantDataToTID> CreateTidData()
    {
        var trackCircuitDatas = await trackCircuitService.GetAllTrackCircuitDataList();
        var switchingMachines = await switchingMachineService.GetAllSwitchingMachines();
        var switchingMachineDatas = switchingMachines.Select(SwitchingMachineService.ToSwitchData).ToList();
        var directionDatas = await directionRouteService.GetAllDirectionData();

        return new()
        {
            TrackCircuitDatas = trackCircuitDatas,
            SwitchDatas = switchingMachineDatas,
            DirectionDatas = directionDatas
        };
    }
}
