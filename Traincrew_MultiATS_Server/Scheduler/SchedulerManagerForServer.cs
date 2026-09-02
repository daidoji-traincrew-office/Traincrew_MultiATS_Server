using Traincrew_MultiATS_Server.Repositories.Mutex;

namespace Traincrew_MultiATS_Server.Scheduler;

public class SchedulerManagerForServer(
    IServiceScopeFactory serviceScopeFactory,
    IMutexRepository mutexRepository
): SchedulerManager(mutexRepository)
{
    private ServerModeScheduler? _serverModeScheduler;
    private MetricsCollectorScheduler? _metricsCollectorScheduler;

    protected override string MutexKey => nameof(SchedulerManagerForServer);

    protected override List<Scheduler> InitSchedulers()
    {
        return [
            new OperationNotificationScheduler(serviceScopeFactory),
            new InterlockingHubScheduler(serviceScopeFactory),
            new TIDHubScheduler(serviceScopeFactory),
            new CTCPHubScheduler(serviceScopeFactory),
            new CommanderTableHubScheduler(serviceScopeFactory),
            new DestinationButtonScheduler(serviceScopeFactory),
            new TrainScheduler(serviceScopeFactory),
            new SignalScheduler(serviceScopeFactory)
        ];
    }

    public void StartServerModeScheduler()
    {
        _serverModeScheduler ??= new(serviceScopeFactory);
        _metricsCollectorScheduler ??= new(serviceScopeFactory);
    }

    public async Task StopServerModeScheduler()
    {
        if (_metricsCollectorScheduler != null)
        {
            await _metricsCollectorScheduler.Stop();
            _metricsCollectorScheduler = null;
        }
        if (_serverModeScheduler != null)
        {
            await _serverModeScheduler.Stop();
            _serverModeScheduler = null;
        }
    }
}
