using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class RendoScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 250;
    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var service = scope.ServiceProvider.GetRequiredService<RendoService>();
        using (activity?.Source.StartActivity($"{GetType()}.LeverToRouteState"))
        {
            await service.LeverToRouteState();
        }
        using (activity?.Source.StartActivity($"{GetType()}.DirectionRelay"))
        {
            await service.DirectionRelay();
        }
        using (activity?.Source.StartActivity($"{GetType()}.RouteLockRelay"))
        {
            await service.RouteLockRelay();
        }
        using (activity?.Source.StartActivity($"{GetType()}.RouteRelayWithoutSwitchingMachine"))
        {
            await service.RouteRelayWithoutSwitchingMachine();
        }
        using (activity?.Source.StartActivity($"{GetType()}.RouteRelay"))
        {
            await service.RouteRelay();
        }
        using (activity?.Source.StartActivity($"{GetType()}.SignalControl"))
        {
            await service.SignalControl();
        }
        using (activity?.Source.StartActivity($"{GetType()}.ApproachLockRelay"))
        {
            await service.ApproachLockRelay();
        }
        using (activity?.Source.StartActivity($"{GetType()}.TimerRelay"))
        {
            await service.TimerRelay();
        }
    }
}