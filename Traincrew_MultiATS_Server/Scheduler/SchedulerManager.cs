using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Mutex;

namespace Traincrew_MultiATS_Server.Scheduler;

public class SchedulerManager(
    IServiceScopeFactory serviceScopeFactory,
    IMutexRepository mutexRepository
)
{
    private bool _isRunning;
    private readonly Dictionary<string, Scheduler> _schedulers = new();
    private readonly Dictionary<string, bool> _schedulerStates = new();
    private ServerModeScheduler? _serverModeScheduler;
    
    private void InitSchedulers()
    {
        var schedulerInstances = new List<Scheduler>
        {
            new SwitchingMachineScheduler(serviceScopeFactory),
            new RendoScheduler(serviceScopeFactory),
            new OperationNotificationScheduler(serviceScopeFactory),
            new TtcStationControlScheduler(serviceScopeFactory),
            new InterlockingHubScheduler(serviceScopeFactory),
            new TIDHubScheduler(serviceScopeFactory),
            new CommanderTableHubScheduler(serviceScopeFactory),
            new DestinationButtonScheduler(serviceScopeFactory)
        };

        foreach (var scheduler in schedulerInstances)
        {
            var name = scheduler.GetType().Name;
            _schedulers[name] = scheduler;
            _schedulerStates[name] = true;
        }
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
        await Task.WhenAll(_schedulers.Values.Select(s => s.Stop()));
        _schedulers.Clear();
        _schedulerStates.Clear();
        _isRunning = false;
    }

    public List<SchedulerInfo> GetSchedulers()
    {
        return _schedulers.Select(kvp => new SchedulerInfo
        {
            Name = kvp.Key,
            IsEnabled = _schedulerStates.GetValueOrDefault(kvp.Key, false),
            Type = kvp.Value.GetType().Name
        }).ToList();
    }

    public async Task<bool> ToggleScheduler(string schedulerName, bool isEnabled)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(SchedulerManager));
        
        if (!_schedulers.ContainsKey(schedulerName))
        {
            return false;
        }

        _schedulerStates[schedulerName] = isEnabled;

        if (!isEnabled)
        {
            await _schedulers[schedulerName].Stop();
            _schedulers.Remove(schedulerName);
        }
        else if (_isRunning)
        {
            var newScheduler = CreateSchedulerInstance(schedulerName);
            if (newScheduler != null)
            {
                _schedulers[schedulerName] = newScheduler;
            }
        }

        return true;
    }

    private Scheduler? CreateSchedulerInstance(string schedulerName)
    {
        return schedulerName switch
        {
            nameof(SwitchingMachineScheduler) => new SwitchingMachineScheduler(serviceScopeFactory),
            nameof(RendoScheduler) => new RendoScheduler(serviceScopeFactory),
            nameof(OperationNotificationScheduler) => new OperationNotificationScheduler(serviceScopeFactory),
            nameof(TtcStationControlScheduler) => new TtcStationControlScheduler(serviceScopeFactory),
            nameof(InterlockingHubScheduler) => new InterlockingHubScheduler(serviceScopeFactory),
            nameof(TIDHubScheduler) => new TIDHubScheduler(serviceScopeFactory),
            nameof(CommanderTableHubScheduler) => new CommanderTableHubScheduler(serviceScopeFactory),
            nameof(DestinationButtonScheduler) => new DestinationButtonScheduler(serviceScopeFactory),
            _ => null
        };
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