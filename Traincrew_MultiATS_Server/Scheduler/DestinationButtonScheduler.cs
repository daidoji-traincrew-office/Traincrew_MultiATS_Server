using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class DestinationButtonScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 500;

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var interlockingService = scope.ServiceProvider.GetRequiredService<IInterlockingService>();
        await interlockingService.ResetRaisedButtonsAsync();
    }
}
