using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Services;

public class CommanderTableService(
    TrackCircuitService trackCircuitService,
    OperationNotificationService operationNotificationService,
    OperationInformationService operationInformationService,
    ProtectionService protectionService,
    TrainService trainService
)
{
    public async Task<DataToCommanderTable> SendData_CommanderTable()
    {
        return new()
        {
            TroubleDataList = [],
            OperationNotificationDataList = await operationNotificationService.GetOperationNotificationData(),
            TrackCircuitDataList = await trackCircuitService.GetAllTrackCircuitDataList(),
            OperationInformationDataList = await operationInformationService.GetAllOperationInformations(),
            ProtectionZoneDataList = await protectionService.GetProtectionZoneStates(),
            TrainStateDataList = await trainService.GetAllTrainState(),
        };
    }
}