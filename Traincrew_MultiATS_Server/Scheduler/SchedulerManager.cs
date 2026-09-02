using Traincrew_MultiATS_Server.Repositories.Mutex;

namespace Traincrew_MultiATS_Server.Scheduler;

public abstract class SchedulerManager(IMutexRepository mutexRepository)
{
    private bool _isRunning;
    private List<Scheduler> _schedulers = [];

    protected abstract List<Scheduler> InitSchedulers();

    protected abstract string MutexKey { get; }

    public async Task Start()
    {
        await using var mutex = await mutexRepository.AcquireAsync(MutexKey);
        if (_isRunning)
        {
            return;
        }

        _schedulers = InitSchedulers();
        _isRunning = true;
    }

    public async Task Stop()
    {
        await using var mutex = await mutexRepository.AcquireAsync(MutexKey);
        if (!_isRunning)
        {
            return;
        }

        await Task.WhenAll(_schedulers.Select(s => s.Stop()));
        _schedulers.Clear();
        _isRunning = false;
    }
}
