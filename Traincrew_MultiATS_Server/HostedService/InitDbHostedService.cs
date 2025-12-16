using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Initialization;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Scheduler;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.HostedService;

/// <summary>
/// Hosted service for database initialization at application startup
/// Coordinates initialization and scheduler management
/// </summary>
public class InitDbHostedService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<InitDbHostedService> _logger;

    public InitDbHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ILoggerFactory loggerFactory,
        ILogger<InitDbHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Called when the application starts
    /// Initializes the database and starts the scheduler
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("InitDbHostedService starting");

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var datetimeRepository = scope.ServiceProvider.GetRequiredService<IDateTimeRepository>();
        var lockConditionRepository = scope.ServiceProvider.GetRequiredService<ILockConditionRepository>();
        var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();
        var schedulerManager = scope.ServiceProvider.GetRequiredService<SchedulerManager>();

        // Initialize database using orchestrator
        var orchestrator = new DatabaseInitializationOrchestrator(
            context,
            datetimeRepository,
            lockConditionRepository,
            _loggerFactory,
            _loggerFactory.CreateLogger<DatabaseInitializationOrchestrator>());

        await orchestrator.InitializeAsync(cancellationToken);

        // Start server mode scheduler
        schedulerManager.StartServerModeScheduler();
        await serverService.UpdateSchedulerAsync();

        _logger.LogInformation("InitDbHostedService started successfully");
    }

    /// <summary>
    /// Called when the application stops
    /// Stops the scheduler
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("InitDbHostedService stopping");

        using var scope = _serviceScopeFactory.CreateScope();
        var schedulerManager = scope.ServiceProvider.GetRequiredService<SchedulerManager>();

        schedulerManager.Stop();
        schedulerManager.StopServerModeScheduler();

        _logger.LogInformation("InitDbHostedService stopped");

        await Task.CompletedTask;
    }
}
