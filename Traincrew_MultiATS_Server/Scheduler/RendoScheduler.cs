using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class RendoScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 250;
    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var service = scope.ServiceProvider.GetRequiredService<RendoService>();
        await service.LeverToRouteState();
        activity?.AddEvent(new("RendoScheduler: LeverToRouteState executed"));
        await service.DirectionRelay();
        activity?.AddEvent(new("RendoScheduler: DirectionRelay executed"));
        await service.RouteLockRelay();
        activity?.AddEvent(new("RendoScheduler: RouteLockRelay executed"));
        await service.RouteRelayWithoutSwitchingMachine();
        activity?.AddEvent(new("RendoScheduler: RouteRelayWithoutSwitchingMachine executed"));
        await service.RouteRelay();
        activity?.AddEvent(new("RendoScheduler: RouteRelay executed"));
        await service.SignalControl();
        activity?.AddEvent(new("RendoScheduler: SignalControl executed"));
        await service.ApproachLockRelay();
        activity?.AddEvent(new("RendoScheduler: ApproachLockRelay executed"));
        await service.TimerRelay();
        activity?.AddEvent(new("RendoScheduler: TimerRelay executed"));
    }
}