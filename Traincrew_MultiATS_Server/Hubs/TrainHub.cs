using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class TrainHub(TrackCircuitService trackCircuitService, ProtectionService protectionService) : Hub
{
    public async Task<DataFromServer> SendData_ATS(DataToServer clientData)
    {
        DataFromServer serverData = new DataFromServer();
        List<TrackCircuitData> old_TrackCircuitDataList =
            await trackCircuitService.GetTrackCircuitDataListByTrainNumber(clientData.DiaName);
        List<TrackCircuitData> Incremental_TrackCircuitDataList =
            clientData.OnTrackList.Except(old_TrackCircuitDataList).ToList();
        List<TrackCircuitData> Decremental_TrackCircuitDataList =
            old_TrackCircuitDataList.Except(clientData.OnTrackList).ToList();
        List<int> bougo_zone = await trackCircuitService.GetBougoZoneByTrackCircuitDataList(clientData.OnTrackList);
        bool BougoState = await protectionService.GetProtectionZoneStateByBougoZone(bougo_zone);
        if (BougoState == true)
        {
            serverData.BougoState = true;
        }
        if (clientData.BougoState == true)
        {
            foreach (var item in bougo_zone)
            {
                await protectionService.EnableProtection(clientData.DiaName, item);
            }
        }
        if(clientData.BougoState == false)
        {
            await protectionService.DisableProtection(clientData.DiaName);
        }
        if(clientData.BougoState == false)
        await trackCircuitService.SetTrackCircuitDataList(Incremental_TrackCircuitDataList, clientData.DiaName);
        await trackCircuitService.ClearTrackCircuitDataList(Decremental_TrackCircuitDataList);
        return serverData;
    }
}