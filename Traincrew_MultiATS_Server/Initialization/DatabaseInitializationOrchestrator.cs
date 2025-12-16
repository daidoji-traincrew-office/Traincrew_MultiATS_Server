using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.LockCondition;

namespace Traincrew_MultiATS_Server.Initialization;

/// <summary>
///     Orchestrates the database initialization sequence
///     Coordinates CSV loading and database initialization in the correct order
/// </summary>
public class DatabaseInitializationOrchestrator(
    ApplicationDbContext context,
    IDateTimeRepository datetimeRepository,
    ILockConditionRepository lockConditionRepository,
    ILoggerFactory loggerFactory,
    ILogger<DatabaseInitializationOrchestrator> logger)
{
    /// <summary>
    ///     Execute the complete database initialization sequence
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting database initialization");

        // Phase 1: Load stationList
        var stationLoader = new StationCsvLoader(loggerFactory.CreateLogger<StationCsvLoader>());
        var stationList = await stationLoader.LoadAsync(cancellationToken);

        // Phase 2: Initialize stations
        var stationInitializer = new StationDbInitializer(context, loggerFactory.CreateLogger<StationDbInitializer>());
        await stationInitializer.InitializeStationsAsync(stationList, cancellationToken);
        await stationInitializer.InitializeStationTimerStatesAsync(cancellationToken);

        // Phase 3: Load trackCircuitList
        var trackCircuitLoader = new TrackCircuitCsvLoader(loggerFactory.CreateLogger<TrackCircuitCsvLoader>());
        var trackCircuitList = await trackCircuitLoader.LoadAsync(cancellationToken);

        // Phase 4: Initialize track circuits
        var trackCircuitInitializer = new TrackCircuitDbInitializer(context, loggerFactory.CreateLogger<TrackCircuitDbInitializer>());
        await trackCircuitInitializer.InitializeTrackCircuitsAsync(trackCircuitList, cancellationToken);

        // Phase 5: Load signalTypeList
        var signalTypeLoader = new SignalTypeCsvLoader(loggerFactory.CreateLogger<SignalTypeCsvLoader>());
        var signalTypeList = await signalTypeLoader.LoadAsync(cancellationToken);

        // Phase 6: Initialize signal types
        var signalTypeInitializer = new SignalTypeDbInitializer(context, loggerFactory.CreateLogger<SignalTypeDbInitializer>());
        await signalTypeInitializer.InitializeSignalTypesAsync(signalTypeList, cancellationToken);

        // Phase 7: Load train data
        var trainTypeLoader = new TrainTypeCsvLoader(loggerFactory.CreateLogger<TrainTypeCsvLoader>());
        var trainTypeList = trainTypeLoader.Load();

        var trainDiagramLoader = new TrainDiagramCsvLoader(loggerFactory.CreateLogger<TrainDiagramCsvLoader>());
        var trainDiagramList = trainDiagramLoader.Load();

        // Phase 8: Initialize train data
        var trainInitializer = new TrainDbInitializer(context, loggerFactory.CreateLogger<TrainDbInitializer>());
        await trainInitializer.InitializeTrainTypesAsync(trainTypeList, cancellationToken);
        await trainInitializer.InitializeTrainDiagramsAsync(trainDiagramList, cancellationToken);

        // Phase 9: Initialize Rendo Table objects (per station)
        var rendoTableInitializers = await CreateRendoTableInitializersAsync(cancellationToken);
        foreach (var initializer in rendoTableInitializers)
        {
            await initializer.InitializeObjects();
        }

        DetachUnchangedEntities();

        // Phase 10: Load operational data
        var operationNotificationDisplayCsvLoader = new OperationNotificationDisplayCsvLoader(loggerFactory.CreateLogger<OperationNotificationDisplayCsvLoader>());
        var routeLockTrackCircuitCsvLoader = new RouteLockTrackCircuitCsvLoader(loggerFactory.CreateLogger<RouteLockTrackCircuitCsvLoader>());
        var ttcWindowCsvLoader = new TtcWindowCsvLoader(loggerFactory.CreateLogger<TtcWindowCsvLoader>());
        var ttcWindowLinkCsvLoader = new TtcWindowLinkCsvLoader(loggerFactory.CreateLogger<TtcWindowLinkCsvLoader>());
        var throwOutControlCsvLoader = new ThrowOutControlCsvLoader(loggerFactory.CreateLogger<ThrowOutControlCsvLoader>());

        // Phase 11: Initialize operational entities
        var operationNotificationInitializer = new OperationNotificationDisplayDbInitializer(
            context,
            datetimeRepository,
            loggerFactory.CreateLogger<OperationNotificationDisplayDbInitializer>(),
            operationNotificationDisplayCsvLoader);
        await operationNotificationInitializer.InitializeAsync(cancellationToken);

        var routeInitializer = new RouteDbInitializer(context, loggerFactory.CreateLogger<RouteDbInitializer>(), routeLockTrackCircuitCsvLoader);
        await routeInitializer.InitializeAsync(cancellationToken);

        var serverStatusInitializer =
            new ServerStatusDbInitializer(context, loggerFactory.CreateLogger<ServerStatusDbInitializer>());
        await serverStatusInitializer.InitializeAsync(cancellationToken);

        var ttcInitializer = new TtcDbInitializer(context, loggerFactory.CreateLogger<TtcDbInitializer>(), ttcWindowCsvLoader, ttcWindowLinkCsvLoader);
        await ttcInitializer.InitializeAsync(cancellationToken);

        var throwOutControlInitializer = new ThrowOutControlDbInitializer(
            context,
            loggerFactory.CreateLogger<ThrowOutControlDbInitializer>(),
            throwOutControlCsvLoader);
        await throwOutControlInitializer.InitializeAsync(cancellationToken);

        // Phase 12: Load signal data
        var signalLoader = new SignalCsvLoader(loggerFactory.CreateLogger<SignalCsvLoader>());
        var signalDataList = signalLoader.Load();

        // Phase 13: Initialize signals and routes
        var signalInitializer = new SignalDbInitializer(context, loggerFactory.CreateLogger<SignalDbInitializer>());
        await signalInitializer.InitializeSignalsAsync(signalDataList, cancellationToken);
        await signalInitializer.InitializeNextSignalsAsync(signalDataList, cancellationToken);
        await signalInitializer.InitializeSignalRoutesAsync(signalDataList, cancellationToken);

        // Phase 14: Initialize track circuit signals
        await trackCircuitInitializer.InitializeTrackCircuitSignalsAsync(trackCircuitList, cancellationToken);

        DetachUnchangedEntities();

        // Phase 15: Initialize locks (per station)
        foreach (var initializer in rendoTableInitializers)
        {
            await initializer.InitializeLocks();
        }

        // Phase 16: Finalize initialization
        DetachUnchangedEntities();
        await FinalizeInitializationAsync(cancellationToken);

        logger.LogInformation("Database initialization completed");
    }

    private async Task<List<DbRendoTableInitializer>> CreateRendoTableInitializersAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating Rendo Table initializers");

        var rendoTableLoader = new RendoTableCsvLoader(loggerFactory.CreateLogger<RendoTableCsvLoader>());
        var rendoTableData = await rendoTableLoader.LoadAllAsync(cancellationToken);

        var initializers = new List<DbRendoTableInitializer>();
        foreach (var (stationId, csvData) in rendoTableData)
        {
            var initializer = new DbRendoTableInitializer(
                stationId,
                csvData,
                context,
                datetimeRepository,
                loggerFactory.CreateLogger<DbRendoTableInitializer>(),
                cancellationToken);
            initializers.Add(initializer);
        }

        logger.LogInformation("Created {Count} Rendo Table initializers", initializers.Count);
        return initializers;
    }

    private async Task FinalizeInitializationAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Finalizing initialization");

        var switchingMachineRouteInitializer = new SwitchingMachineRouteDbInitializer(
            context,
            lockConditionRepository,
            loggerFactory.CreateLogger<SwitchingMachineRouteDbInitializer>());
        await switchingMachineRouteInitializer.SetStationIdToInterlockingObjectAsync(cancellationToken);
        await switchingMachineRouteInitializer.InitializeSwitchingMachineRoutesAsync(cancellationToken);
    }

    private void DetachUnchangedEntities()
    {
        var unchangedEntries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Unchanged)
            .ToList();

        foreach (var entry in unchangedEntries)
        {
            entry.State = EntityState.Detached;
        }

        logger.LogDebug("Detached {Count} unchanged entities", unchangedEntries.Count);
    }
}