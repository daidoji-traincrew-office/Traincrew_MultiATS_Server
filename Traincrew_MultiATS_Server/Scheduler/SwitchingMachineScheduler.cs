using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class SwitchingMachineScheduler(IServiceScopeFactory serviceScopeFactory): Scheduler(serviceScopeFactory)
{
    protected override int Interval => 100;

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var service = scope.ServiceProvider.GetRequiredService<SwitchingMachineService>();
        await service.SwitchingMachineControl();
    }
}