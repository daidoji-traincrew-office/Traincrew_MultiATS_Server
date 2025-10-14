using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class RendoScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 250;
    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var service = scope.ServiceProvider.GetRequiredService<RendoService>();
        using (activity?.Source.StartActivity($"{GetType().Name}.CHRRelay"))
        {
            await service.CHRRelay();
        }
        using (activity?.Source.StartActivity($"{GetType().Name}.LeverToRouteState"))
        {
            await service.LeverToRouteState();
        }
        using (activity?.Source.StartActivity($"{GetType().Name}.DirectionRelay"))
        {
            await service.DirectionRelay();
        }
        using (activity?.Source.StartActivity($"{GetType().Name}.RouteLockRelay"))
        {
            await service.RouteLockRelay();
        }
        using (activity?.Source.StartActivity($"{GetType().Name}.RouteRelayWithoutSwitchingMachine"))
        {
            await service.RouteRelayWithoutSwitchingMachine();
        }
        using (activity?.Source.StartActivity($"{GetType().Name}.RouteRelay"))
        {
            await service.RouteRelay();
        }
        using (activity?.Source.StartActivity($"{GetType().Name}.SRelay"))
        {
            await service.SRelay();
        }
        using (activity?.Source.StartActivity($"{GetType().Name}.SignalControl"))
        {
            await service.SignalControl();
        }
        using (activity?.Source.StartActivity($"{GetType().Name}.ApproachLockRelay"))
        {
            await service.ApproachLockRelay();
        }
        using (activity?.Source.StartActivity($"{GetType().Name}.TimerRelay"))
        {
            await service.TimerRelay();
        }
    }
}