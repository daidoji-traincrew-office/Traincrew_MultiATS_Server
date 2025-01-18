using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class TrainHub(TrackCircuitService trackCircuitService): Hub
{
   public async Task SendData_ATS(DataToServer clientData)
   {
      List<TrackCircuitData> old_TrackCircuitDataList = await trackCircuitService.GetTrackCircuitDataListByTrainNumber(clientData.DiaName);
      List<TrackCircuitData> Incremental_TrackCircuitDataList = clientData.OnTrackList.Except(old_TrackCircuitDataList).ToList();
      List<TrackCircuitData> Decremental_TrackCircuitDataList = old_TrackCircuitDataList.Except(clientData.OnTrackList).ToList();
      
      DataFromServer serverData = new DataFromServer();
      Console.WriteLine("Hello" + clientData.DiaName);
      await Clients.Caller.SendAsync("ReceiveData", serverData);
   }
}