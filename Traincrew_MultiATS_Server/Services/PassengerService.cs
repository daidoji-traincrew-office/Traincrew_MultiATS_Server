using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Services;

public class PassengerService(
    TrackCircuitService trackCircuitService,
    TrainService trainService)
{
    public async Task<ServerToPassengerData>  GetServerToPassengerData()
    {
        var trackCircuitData = await trackCircuitService.GetShortCircuitedTrackCircuitDataList();
        var trainInfoByTrainNumber = await trainService.GetTrainInfoByTrainNumber();

        return new()
        {
            TrackCircuitData = trackCircuitData,
            TrainInfos = trainInfoByTrainNumber
        };
    }
}