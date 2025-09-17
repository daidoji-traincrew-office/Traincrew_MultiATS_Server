using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Scheduler;

namespace Traincrew_MultiATS_Server.Services;

public class SchedulerService(SchedulerManager schedulerManager)
{
    public async Task<List<SchedulerInfo>> GetSchedulers()
    {
        return schedulerManager.GetSchedulers();
    }
    
    public async Task<bool> ToggleScheduler(string schedulerName, bool isEnabled)
    {
        if (schedulerName == "ServerModeScheduler")
        {
            if (isEnabled)
            {
                schedulerManager.StartServerModeScheduler();
            }
            else
            {
                await schedulerManager.StopServerModeScheduler();
            }
            return true;
        }
        
        return await schedulerManager.ToggleScheduler(schedulerName, isEnabled);
    }
    
    public async Task<SchedulerInfo?> GetSchedulerInfo(string schedulerName)
    {
        var schedulers = await GetSchedulers();
        return schedulers.FirstOrDefault(s => s.Name == schedulerName);
    }
}