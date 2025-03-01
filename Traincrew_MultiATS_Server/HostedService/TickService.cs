using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.HostedService;

public class TickService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _task;

    public TickService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _task = Task.Run(async () => await RunAsync(_cancellationTokenSource.Token));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var timer = Task.Delay(100, cancellationToken);
            await ExecuteTaskAsync();
            await timer;
        }
    }

    private async Task ExecuteTaskAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<SwitchingMachineService>();
        await service.SwitchingMachineControl();
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _task.Wait();
    }
}
