using Traincrew_MultiATS_Server.Repositories.Mutex;

namespace Traincrew_MultiATS_Server.Scheduler;

public class SchedulerManager(
    IServiceScopeFactory serviceScopeFactory,
    IMutexRepository mutexRepository
)
{
    private bool _isRunning;

    private void Hoge()
    {
        List<Scheduler> schedulers =
        [
            new SwitchingMachineScheduler(serviceScopeFactory),
            new RendoScheduler(serviceScopeFactory),
            new OperationNotificationScheduler(serviceScopeFactory),
            new TtcStationControlScheduler(serviceScopeFactory),
            new InterlockingHubScheduler(serviceScopeFactory),
            new TIDHubScheduler(serviceScopeFactory),
            new DestinationButtonScheduler(serviceScopeFactory)
        ];
    }
    public async Task Start()
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(SchedulerManager));
        if (_isRunning)
        {
            return;
        }
        // Todo: Schedulerの初期化処理をここに追加

        _isRunning = true;
    }

    public async Task Stop()
    {
       
        await using var mutex = await mutexRepository.AcquireAsync(nameof(SchedulerManager));
        if (!_isRunning)
        {
            return;
        }
        // Todo: Schedulerの停止処理をここに追加

        _isRunning = false;
    }
}