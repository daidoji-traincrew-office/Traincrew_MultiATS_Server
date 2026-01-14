using Traincrew_MultiATS_Server.Initialization;
using Traincrew_MultiATS_Server.Scheduler;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.HostedService;

/// <summary>
/// Hosted service for database initialization at application startup
/// Coordinates initialization and scheduler management
/// </summary>
public class InitDbHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<InitDbHostedService> logger)
    : IHostedService
{
    /// <summary>
    /// Called when the application starts
    /// Initializes the database and starts the scheduler
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("InitDbHostedService starting");

        // Initialize database using orchestrator from DI container
        using var scope = serviceScopeFactory.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<DatabaseInitializationOrchestrator>();
        await orchestrator.InitializeAsync(cancellationToken);

        // Start server mode scheduler
        var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();
        var schedulerManager = scope.ServiceProvider.GetRequiredService<SchedulerManager>();

        schedulerManager.StartServerModeScheduler();
        await serverService.UpdateSchedulerAsync();

        logger.LogInformation("InitDbHostedService started successfully");
    }

    /// <summary>
    /// Called when the application stops
    /// Stops the scheduler
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("InitDbHostedService stopping");

        using var scope = serviceScopeFactory.CreateScope();
        var schedulerManager = scope.ServiceProvider.GetRequiredService<SchedulerManager>();

        await schedulerManager.Stop();
        await schedulerManager.StopServerModeScheduler();

        logger.LogInformation("InitDbHostedService stopped");

        await Task.CompletedTask;
    }
}