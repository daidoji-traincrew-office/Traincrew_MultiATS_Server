using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Services;

public interface ICommanderTableService
{
    Task<DataToCommanderTable> BuildCommanderTableDataAsync(CommonReads commonReads);
}

public class CommanderTableService(
    IOperationNotificationService operationNotificationService,
    IOperationInformationService operationInformationService,
    IProtectionService protectionService,
    IServerService serverService,
    IBannedUserService bannedUserService
) : ICommanderTableService
{
    /// <summary>
    /// <see cref="CommonReads"/> から司令卓配信用データを組み立てる。
    /// mutexもトランザクションも張らない(呼び出し元が既に管理している前提)。
    /// </summary>
    public async Task<DataToCommanderTable> BuildCommanderTableDataAsync(CommonReads commonReads)
    {
        return new()
        {
            TroubleDataList = [],
            OperationNotificationDataList = await operationNotificationService.GetOperationNotificationData(),
            TrackCircuitDataList = commonReads.TrackCircuits,
            OperationInformationDataList = await operationInformationService.GetAllOperationInformations(),
            ProtectionRadioDataList = await protectionService.GetProtectionRadioStates(),
            TrainStateDataList = commonReads.TrainStates,
            TimeOffset = commonReads.TimeOffset,
            BannedUserIdList = await bannedUserService.GetBannedUserIdsAsync(),
            SelectedDiagramId = await serverService.GetSelectedDiagramIdAsync(),
        };
    }
}