using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Services;

public class TIDService(
    TrackCircuitService trackCircuitService,
    SwitchingMachineService switchingMachineService,
    DirectionRouteService directionRouteService,
    TrainService trainService)
{
    public async Task<ConstantDataToTID> CreateTidData()
    {
        var trackCircuitDatas = await trackCircuitService.GetAllTrackCircuitDataList();
        var switchingMachineDatas = await switchingMachineService.GetAllSwitchData();
        var directionDatas = await directionRouteService.GetAllDirectionData();
        var trainStateDatas = await trainService.GetAllTrainState();

        return new()
        {
            TrackCircuitDatas = trackCircuitDatas,
            SwitchDatas = switchingMachineDatas,
            DirectionDatas = directionDatas,
            TrainStateDatas = trainStateDatas
        };
    }
}
