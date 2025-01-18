using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class TrainHub(TrackCircuitService trackCircuitService) : Hub
{
    public async Task<DataFromServer> SendData_ATS(DataToServer clientData)
    {
        List<TrackCircuitData> old_TrackCircuitDataList =
            await trackCircuitService.GetTrackCircuitDataListByTrainNumber(clientData.DiaName);
        List<TrackCircuitData> Incremental_TrackCircuitDataList =
            clientData.OnTrackList.Except(old_TrackCircuitDataList).ToList();
        List<TrackCircuitData> Decremental_TrackCircuitDataList =
            old_TrackCircuitDataList.Except(clientData.OnTrackList).ToList();
        await trackCircuitService.SetTrackCircuitDataList(Incremental_TrackCircuitDataList, clientData.DiaName);
        await trackCircuitService.ClearTrackCircuitDataList(Decremental_TrackCircuitDataList);
        DataFromServer serverData = new DataFromServer();
        return serverData;
    }
}