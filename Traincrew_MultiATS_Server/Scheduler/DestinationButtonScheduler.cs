using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class DestinationButtonScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 500;

    protected override async Task ExecuteTaskAsync(IServiceScope scope)
    {
        var interlockingService = scope.ServiceProvider.GetRequiredService<InterlockingService>();
        await interlockingService.ResetRaisedButtonsAsync();
    }
}
