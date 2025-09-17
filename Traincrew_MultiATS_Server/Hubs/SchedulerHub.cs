using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme
)]
public class SchedulerHub(SchedulerService schedulerService) : Hub<ISchedulerClientContract>, ISchedulerHubContract
{
    public async Task<List<SchedulerInfo>> GetSchedulers()
    {
        return await schedulerService.GetSchedulers();
    }

    public async Task<bool> ToggleScheduler(string schedulerName, bool isEnabled)
    {
        var result = await schedulerService.ToggleScheduler(schedulerName, isEnabled);
        
        if (result)
        {
            var updatedScheduler = await schedulerService.GetSchedulerInfo(schedulerName);
            if (updatedScheduler != null)
            {
                await Clients.All.ReceiveSchedulerStatusUpdate(updatedScheduler);
            }
        }
        
        return result;
    }
}