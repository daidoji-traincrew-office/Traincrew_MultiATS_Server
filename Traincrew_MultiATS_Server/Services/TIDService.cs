using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Services;

public interface ITIDService
{
    Task<ConstantDataToTID> CreateTidData();
}

public class TIDService(
    ITrackCircuitService trackCircuitService,
    ISwitchingMachineService switchingMachineService,
    IDirectionRouteService directionRouteService,
    ITrainService trainService,
    IServerService serverService) : ITIDService
{
    public async Task<ConstantDataToTID> CreateTidData()
    {
        var trackCircuitDatas = await trackCircuitService.GetAllTrackCircuitDataList();
        var switchingMachineDatas = await switchingMachineService.GetAllSwitchData();
        var directionDatas = await directionRouteService.GetAllDirectionData();
        var trainStateDatas = await trainService.GetAllTrainState();
        var timeOffset = await serverService.GetTimeOffsetAsync();

        return new()
        {
            TrackCircuitDatas = trackCircuitDatas,
            SwitchDatas = switchingMachineDatas,
            DirectionDatas = directionDatas,
            TrainStateDatas = trainStateDatas,
            TimeOffset = timeOffset
        };
    }
}
