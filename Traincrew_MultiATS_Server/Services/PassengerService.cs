using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Services;

public interface IPassengerService
{
    Task<ServerToPassengerData> GetServerToPassengerData();
}

public class PassengerService(
    IServerService serverService,
    ITrackCircuitService trackCircuitService,
    ITrainService trainService,
    IOperationInformationService operationInformationService
) : IPassengerService
{
    public async Task<ServerToPassengerData> GetServerToPassengerData()
    {
        var serverMode = ServerMode.Public == await serverService.GetServerModeAsync();
        var trackCircuitData = await trackCircuitService.GetShortCircuitedTrackCircuitDataList();
        var trainInfoByTrainNumber = await trainService.GetTrainInfoGroupByTrainNumber();
        var operationInformations = await operationInformationService.GetOperationInformations();

        return new()
        {
            ServerMode = serverMode,
            TrackCircuitData = trackCircuitData,
            TrainInfos = trainInfoByTrainNumber,
            OperationInformations = operationInformations
        };
    }
}