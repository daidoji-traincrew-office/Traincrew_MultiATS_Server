using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class RendoScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 250;
    protected override async Task ExecuteTaskAsync(IServiceScope scope)
    {
        var service = scope.ServiceProvider.GetRequiredService<RendoService>();
        await service.LeverToRouteState();
        await service.DirectionRelay();
        await service.RouteLockRelay();
        await service.RouteRelayWithoutSwitchingMachine();
        await service.RouteRelay();
        await service.SignalControl();
        await service.ApproachLockRelay();
        await service.TimerRelay();
    }
}