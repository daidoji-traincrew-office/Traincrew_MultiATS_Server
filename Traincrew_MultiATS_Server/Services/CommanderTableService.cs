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
            TrackCircuitDataList = await trackCircuitService.GetAllTrackCircuitHiddenDataList(),
            OperationInformationDataList = await operationInformationService.GetAllOperationInformations(),
            ProtectionRadioDataList = await protectionService.GetProtectionRadioStates(),
            TrainStateDataList = await trainService.GetAllTrainState(),
        };
    }
}