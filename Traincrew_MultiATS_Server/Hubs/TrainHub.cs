using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

// 運転士 or 車掌使用可能
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "TrainPolicy"
)]
public class TrainHub(
    TrainService trainService) : Hub<ITrainClientContract>, ITrainHubContract
{
    public async Task<ServerToATSData> SendData_ATS(AtsToServerData clientData)
    {
        return await trainService.CreateAtsData(clientData);
    }
}