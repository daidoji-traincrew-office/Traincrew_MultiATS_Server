using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Models;
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

        // Phase 1: Load infrastructure CSV data
        var stationLoader = new StationCsvLoader(loggerFactory.CreateLogger<StationCsvLoader>());
        var trackCircuitLoader = new TrackCircuitCsvLoader(loggerFactory.CreateLogger<TrackCircuitCsvLoader>());
        var signalTypeLoader = new SignalTypeCsvLoader(loggerFactory.CreateLogger<SignalTypeCsvLoader>());

        var stationList = await stationLoader.LoadAsync(cancellationToken);
        var trackCircuitList = await trackCircuitLoader.LoadAsync(cancellationToken);
        var signalTypeList = await signalTypeLoader.LoadAsync(cancellationToken);

        var hasInfrastructureData = stationList.Count > 0 && trackCircuitList.Count > 0 && signalTypeList.Count > 0;

        // Phase 2: Initialize infrastructure entities (if CSV data available)
        if (hasInfrastructureData) await InitializeInfrastructureAsync(stationList, trackCircuitList, signalTypeList, cancellationToken);

        // Phase 3: Initialize train data
        await InitializeTrainDataAsync(cancellationToken);

        // Phase 4: Initialize Rendo Table objects (per station)
        var rendoTableInitializers = await CreateRendoTableInitializersAsync(cancellationToken);
        foreach (var initializer in rendoTableInitializers)
        {
            await initializer.InitializeObjects();
        }

        DetachUnchangedEntities();

        // Phase 5: Initialize operational entities
        await InitializeOperationalEntitiesAsync(cancellationToken);

        // Phase 6: Initialize signals and routes
        await InitializeSignalsAndRoutesAsync(cancellationToken);

        // Phase 7: Initialize track circuit signals (if infrastructure data available)
        if (hasInfrastructureData) await InitializeTrackCircuitSignalsAsync(trackCircuitList, cancellationToken);

        DetachUnchangedEntities();

        // Phase 8: Initialize locks (per station)
        foreach (var initializer in rendoTableInitializers)
        {
            await initializer.InitializeLocks();
        }

        // Phase 9: Finalize initialization (if infrastructure data available)
        if (hasInfrastructureData)
        {
            DetachUnchangedEntities();
            await FinalizeInitializationAsync(cancellationToken);
        }

        logger.LogInformation("Database initialization completed");
    }

    private async Task InitializeInfrastructureAsync(
        List<StationCsv> stationList,
        List<TrackCircuitCsv> trackCircuitList,
        List<SignalTypeCsv> signalTypeList,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing infrastructure entities");

        var stationInitializer =
            new StationDbInitializer(context, loggerFactory.CreateLogger<StationDbInitializer>());
        await stationInitializer.InitializeStationsAsync(stationList, cancellationToken);
        await stationInitializer.InitializeStationTimerStatesAsync(cancellationToken);

        var trackCircuitInitializer =
            new TrackCircuitDbInitializer(context, loggerFactory.CreateLogger<TrackCircuitDbInitializer>());
        await trackCircuitInitializer.InitializeTrackCircuitsAsync(trackCircuitList, cancellationToken);

        var signalTypeInitializer =
            new SignalTypeDbInitializer(context, loggerFactory.CreateLogger<SignalTypeDbInitializer>());
        await signalTypeInitializer.InitializeSignalTypesAsync(signalTypeList, cancellationToken);
    }

    private async Task InitializeTrainDataAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing train data");

        var trainTypeLoader = new TrainTypeCsvLoader(loggerFactory.CreateLogger<TrainTypeCsvLoader>());
        var trainTypeList = trainTypeLoader.Load();

        var trainDiagramLoader = new TrainDiagramCsvLoader(loggerFactory.CreateLogger<TrainDiagramCsvLoader>());
        var trainDiagramList = trainDiagramLoader.Load();

        var trainInitializer = new TrainDbInitializer(context, loggerFactory.CreateLogger<TrainDbInitializer>());
        await trainInitializer.InitializeTrainTypesAsync(trainTypeList, cancellationToken);
        await trainInitializer.InitializeTrainDiagramsAsync(trainDiagramList, cancellationToken);
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

    private async Task InitializeOperationalEntitiesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing operational entities");

        var operationNotificationDisplayCsvLoader = new OperationNotificationDisplayCsvLoader(loggerFactory.CreateLogger<OperationNotificationDisplayCsvLoader>());
        var operationNotificationInitializer = new OperationNotificationDisplayDbInitializer(
            context,
            datetimeRepository,
            loggerFactory.CreateLogger<OperationNotificationDisplayDbInitializer>(),
            operationNotificationDisplayCsvLoader);
        await operationNotificationInitializer.InitializeAsync(cancellationToken);

        var routeLockTrackCircuitCsvLoader = new RouteLockTrackCircuitCsvLoader(loggerFactory.CreateLogger<RouteLockTrackCircuitCsvLoader>());
        var routeInitializer = new RouteDbInitializer(context, loggerFactory.CreateLogger<RouteDbInitializer>(), routeLockTrackCircuitCsvLoader);
        await routeInitializer.InitializeAsync(cancellationToken);

        var serverStatusInitializer =
            new ServerStatusDbInitializer(context, loggerFactory.CreateLogger<ServerStatusDbInitializer>());
        await serverStatusInitializer.InitializeAsync(cancellationToken);

        var ttcWindowCsvLoader = new TtcWindowCsvLoader(loggerFactory.CreateLogger<TtcWindowCsvLoader>());
        var ttcWindowLinkCsvLoader = new TtcWindowLinkCsvLoader(loggerFactory.CreateLogger<TtcWindowLinkCsvLoader>());
        var ttcInitializer = new TtcDbInitializer(context, loggerFactory.CreateLogger<TtcDbInitializer>(), ttcWindowCsvLoader, ttcWindowLinkCsvLoader);
        await ttcInitializer.InitializeAsync(cancellationToken);

        var throwOutControlCsvLoader = new ThrowOutControlCsvLoader(loggerFactory.CreateLogger<ThrowOutControlCsvLoader>());
        var throwOutControlInitializer = new ThrowOutControlDbInitializer(
            context,
            loggerFactory.CreateLogger<ThrowOutControlDbInitializer>(),
            throwOutControlCsvLoader);
        await throwOutControlInitializer.InitializeAsync(cancellationToken);
    }

    private async Task InitializeSignalsAndRoutesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing signals and routes");

        var signalLoader = new SignalCsvLoader(loggerFactory.CreateLogger<SignalCsvLoader>());
        var signalDataList = signalLoader.Load();

        var signalInitializer = new SignalDbInitializer(context, loggerFactory.CreateLogger<SignalDbInitializer>());
        await signalInitializer.InitializeSignalsAsync(signalDataList, cancellationToken);
        await signalInitializer.InitializeNextSignalsAsync(signalDataList, cancellationToken);
        await signalInitializer.InitializeSignalRoutesAsync(signalDataList, cancellationToken);
    }

    private async Task InitializeTrackCircuitSignalsAsync(List<TrackCircuitCsv> trackCircuitList,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing track circuit signals");

        var trackCircuitInitializer =
            new TrackCircuitDbInitializer(context, loggerFactory.CreateLogger<TrackCircuitDbInitializer>());
        await trackCircuitInitializer.InitializeTrackCircuitSignalsAsync(trackCircuitList, cancellationToken);
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