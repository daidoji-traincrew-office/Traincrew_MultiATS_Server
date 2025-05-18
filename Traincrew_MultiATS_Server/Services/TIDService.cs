using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Services;

public class TIDService(
    TrackCircuitService trackCircuitService,
    SwitchingMachineService switchingMachineService,
    DirectionRouteService directionRouteService)
{
    public async Task<ConstantDataToTID> CreateTidDataAsync()
    {
        var trackCircuitDataList = await trackCircuitService.GetAllTrackCircuitDataList();
        var switchingMachineDataList = (await switchingMachineService.GetAllSwitchingMachines())
            .Select(SwitchingMachineService.ToSwitchData)
            .ToList();

        var directionDatas = await directionRouteService.GetAllDirectionData();

        return new ConstantDataToTID
        {
            TrackCircuitDatas = trackCircuitDataList,
            SwitchDatas = switchingMachineDataList,
            DirectionDatas = directionDatas
        };
    }
}
