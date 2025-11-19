using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Services;

public interface ICommanderTableService
{
    Task<DataToCommanderTable> SendData_CommanderTable();
}

public class CommanderTableService(
    ITrackCircuitService trackCircuitService,
    IOperationNotificationService operationNotificationService,
    IOperationInformationService operationInformationService,
    IProtectionService protectionService,
    ITrainService trainService,
    IServerService serverService,
    IBannedUserService bannedUserService
) : ICommanderTableService
{
    public async Task<DataToCommanderTable> SendData_CommanderTable()
    {
        return new()
        {
            TroubleDataList = [],
            OperationNotificationDataList = await operationNotificationService.GetOperationNotificationData(),
            TrackCircuitDataList = await trackCircuitService.GetAllTrackCircuitDataList(),
            OperationInformationDataList = await operationInformationService.GetAllOperationInformations(),
            ProtectionRadioDataList = await protectionService.GetProtectionRadioStates(),
            TrainStateDataList = await trainService.GetAllTrainState(),
            TimeOffset = await serverService.GetTimeOffsetAsync(),
            BannedUserIdList = await bannedUserService.GetBannedUserIdsAsync(),
        };
    }
}