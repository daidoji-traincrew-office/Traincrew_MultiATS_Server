using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

public interface ISchedulerHubContract
{
    Task<List<SchedulerInfo>> GetSchedulers();
    Task<bool> ToggleScheduler(string schedulerName, bool isEnabled);
}

public interface ISchedulerClientContract
{
    Task ReceiveSchedulerStatusUpdate(SchedulerInfo schedulerInfo);
}