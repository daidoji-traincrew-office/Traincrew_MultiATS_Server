using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

// 司令員操作可 
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "CommanderTablePolicy"
)]
public class CommanderTableHub(
    TrackCircuitService trackCircuitService,
    OperationNotificationService operationNotificationService,
    TtcStationControlService ttcStationControlService,
    TrainService trainService,
    OperationInformationService operationInformationService,
    ServerService serverService
) : Hub<ICommanderTableClientContract>, ICommanderTableHubContract
{
    public async Task<DataToCommanderTable> SendData_CommanderTable()
    {
        return new()
        {
            TroubleDataList = [],
            OperationNotificationDataList = await operationNotificationService.GetOperationNotificationData(),
            TrackCircuitDataList = await trackCircuitService.GetAllTrackCircuitDataList()
        };
    }

    public async Task SendTroubleData(TroubleData troubleData)
    {

    }

    public async Task SendOperationNotificationData(OperationNotificationData operationNotificationData)
    {
        await operationNotificationService.SetOperationNotificationData(operationNotificationData);
    }

    public async Task SendTrackCircuitData(TrackCircuitData trackCircuitData)
    {
        // 受け取ったtrackCircuitDataの値を設定する
        await trackCircuitService.SetTrackCircuitData(trackCircuitData);
    }

    public async Task DeleteTrain(string trainName)
    {
        await trainService.DeleteTrainState(trainName);
        await trackCircuitService.ClearTrackCircuitByTrainNumber(trainName);
        await ttcStationControlService.ClearTtcWindowByTrainNumber(trainName);
    }

    public async Task<OperationInformationData> AddOperationInformation(OperationInformationData operationInformationData)
    {
        return await operationInformationService.AddOperationInformation(operationInformationData);
    }
    
    public async Task<OperationInformationData> UpdateOperationInformation(OperationInformationData operationInformationData)
    {
        return await operationInformationService.UpdateOperationInformation(operationInformationData);
    }

    public async Task<List<OperationInformationData>> GetAllOperationInformations()
    {
        return await operationInformationService.GetAllOperationInformations();
    }

    public async Task DeleteOperationInformation(long id)
    {
        await operationInformationService.DeleteOperationInformation(id);
    }

    public async Task<ServerMode> GetServerMode()
    {
        return await serverService.GetServerModeAsync();
    }

    public async Task SetServerMode(ServerMode mode)
    {
        await serverService.SetServerModeAsync(mode);
    }
}