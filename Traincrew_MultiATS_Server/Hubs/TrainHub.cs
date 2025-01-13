using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class TrainHub: Hub
{
   public async Task SendData_ATS(DataToServer data)
   {
      DataFromServer serverData = new DataFromServer();
      Console.WriteLine($"{data.DiaName}");
      await Clients.Caller.SendAsync("ReceiveData", serverData);
   }
}