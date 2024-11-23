using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

public class TrainHub: Hub
{
   public Task<int> Emit([FromServices] StationService stationService)
   {
      return Task.FromResult(0);
   }
   public async Task SendData_ATS(DataFromServer sendData_ATS)
   {
      await Clients.Caller.SendAsync("ReceiveData_ATS", sendData_ATS);
   }
}