using Traincrew_MultiATS_Server.Activity;

namespace Traincrew_MultiATS_Server.Scheduler;

public abstract class Scheduler
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _task;
    protected abstract int Interval { get; }

    protected Scheduler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _task = Task.Run(async () => await RunAsync(_cancellationTokenSource.Token));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
            var timer = Task.Delay(Interval, cancellationToken);

            // Activityの開始
            using (var activity = ActivitySources.Scheduler.StartActivity($"{GetType().Name}.ExecuteTask"))
            {
                try
                {
                    await ExecuteTaskAsync(scope, activity);
                }
                catch (System.Exception ex)
                {
                    logger.LogError(ex, "An error occurred while executing the task.");
                    activity?.SetTag("error", true);
                    activity?.SetTag("exception.message", ex.Message);
                    activity?.SetTag("exception.stacktrace", ex.StackTrace);
                }
            }

            try
            {
                await timer;
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    protected abstract Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity);
    

    public async Task Stop()
    {
        await _cancellationTokenSource.CancelAsync();
        await _task;
    }
}
