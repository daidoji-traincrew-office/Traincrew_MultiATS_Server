using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebApplication1.Services;

namespace Traincrew_MultiATS_Server.Hubs;

public class TrainHub: Hub
{
   public Task<int> Emit([FromServices] StationService stationService)
   {
      return Task.FromResult(0);
   } 
}