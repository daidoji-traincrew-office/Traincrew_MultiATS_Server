using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class OperationNotificationScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 500; 
    protected override async Task ExecuteTaskAsync(IServiceScope scope)
    {
        var service = scope.ServiceProvider.GetRequiredService<OperationNotificationService>();
        await service.SetNoneWhereKaijoOrTorikeshiAndSpendMuchTime();
    }
}