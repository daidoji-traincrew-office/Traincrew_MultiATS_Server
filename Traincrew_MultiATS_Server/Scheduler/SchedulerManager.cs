using Traincrew_MultiATS_Server.Repositories.Mutex;

namespace Traincrew_MultiATS_Server.Scheduler;

public class SchedulerManager(
    IServiceScopeFactory serviceScopeFactory,
    IMutexRepository mutexRepository
)
{
    private bool _isRunning;
    private List<Scheduler> _schedulers = [];
    private ServerModeScheduler? _serverModeScheduler;

    private void InitSchedulers()
    {
        _schedulers =
        [
            new SwitchingMachineScheduler(serviceScopeFactory),
            new RendoScheduler(serviceScopeFactory),
            new OperationNotificationScheduler(serviceScopeFactory),
            new TtcStationControlScheduler(serviceScopeFactory),
            new InterlockingHubScheduler(serviceScopeFactory),
            new TIDHubScheduler(serviceScopeFactory),
            new CommanderTableHubScheduler(serviceScopeFactory),
            new DestinationButtonScheduler(serviceScopeFactory),
            new TrainScheduler(serviceScopeFactory)
        ];
    }

    public async Task Start()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(SchedulerManager));
        if (_isRunning)
        {
            return;
        }

        InitSchedulers();
        _isRunning = true;
    }

    public void StartServerModeScheduler()
    {
        _serverModeScheduler ??= new(serviceScopeFactory);
    }

    public async Task Stop()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(SchedulerManager));
        if (!_isRunning)
        {
            return;
        }

        await Task.WhenAll(_schedulers.Select(s => s.Stop()));
        _schedulers.Clear();
        _isRunning = false;
    }

    public async Task StopServerModeScheduler()
    {
        if (_serverModeScheduler != null)
        {
            await _serverModeScheduler.Stop();
            _serverModeScheduler = null;
        }
    }
}