using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class TtcStationControlScheduler(IServiceScopeFactory serviceScopeFactory): Scheduler(serviceScopeFactory)
{
    protected override int Interval => 1000;

    protected override async Task ExecuteTaskAsync(IServiceScope scope)
    {
        var service = scope.ServiceProvider.GetRequiredService<TtcStationControlService>();
        await service.TrainTracking();
    }
}