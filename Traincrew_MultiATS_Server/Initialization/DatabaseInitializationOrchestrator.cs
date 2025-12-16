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

        // Phase 1: StationCsvLoader - 駅一覧の読み込み
        var stationLoader = new StationCsvLoader(loggerFactory.CreateLogger<StationCsvLoader>());
        var stationList = await stationLoader.LoadAsync(cancellationToken);

        // Phase 2: StationDbInitializer - 駅の初期化
        var stationInitializer = new StationDbInitializer(context, loggerFactory.CreateLogger<StationDbInitializer>());
        await stationInitializer.InitializeStationsAsync(stationList, cancellationToken);
        await stationInitializer.InitializeStationTimerStatesAsync(cancellationToken);

        // Phase 3: TrackCircuitCsvLoader - 軌道回路一覧の読み込み
        var trackCircuitLoader = new TrackCircuitCsvLoader(loggerFactory.CreateLogger<TrackCircuitCsvLoader>());
        var trackCircuitList = await trackCircuitLoader.LoadAsync(cancellationToken);

        // Phase 4: TrackCircuitDbInitializer - 軌道回路の初期化
        var trackCircuitInitializer = new TrackCircuitDbInitializer(context, loggerFactory.CreateLogger<TrackCircuitDbInitializer>());
        await trackCircuitInitializer.InitializeTrackCircuitsAsync(trackCircuitList, cancellationToken);

        // Phase 5: SignalTypeCsvLoader - 信号タイプ一覧の読み込み
        var signalTypeLoader = new SignalTypeCsvLoader(loggerFactory.CreateLogger<SignalTypeCsvLoader>());
        var signalTypeList = await signalTypeLoader.LoadAsync(cancellationToken);

        // Phase 6: SignalTypeDbInitializer - 信号タイプの初期化
        var signalTypeInitializer = new SignalTypeDbInitializer(context, loggerFactory.CreateLogger<SignalTypeDbInitializer>());
        await signalTypeInitializer.InitializeSignalTypesAsync(signalTypeList, cancellationToken);

        // Phase 7: TrainTypeCsvLoader - 列車タイプデータの読み込み
        var trainTypeLoader = new TrainTypeCsvLoader(loggerFactory.CreateLogger<TrainTypeCsvLoader>());
        var trainTypeList = trainTypeLoader.Load();

        // Phase 8: TrainDiagramCsvLoader - 列車ダイヤデータの読み込み
        var trainDiagramLoader = new TrainDiagramCsvLoader(loggerFactory.CreateLogger<TrainDiagramCsvLoader>());
        var trainDiagramList = trainDiagramLoader.Load();

        // Phase 9: TrainDbInitializer - 列車データの初期化
        var trainInitializer = new TrainDbInitializer(context, loggerFactory.CreateLogger<TrainDbInitializer>());
        await trainInitializer.InitializeTrainTypesAsync(trainTypeList, cancellationToken);
        await trainInitializer.InitializeTrainDiagramsAsync(trainDiagramList, cancellationToken);

        // Phase 10: DbRendoTableInitializer - 連動表オブジェクトの初期化（駅ごと）
        var rendoTableInitializers = await CreateRendoTableInitializersAsync(cancellationToken);
        foreach (var initializer in rendoTableInitializers)
        {
            await initializer.InitializeObjects();
        }

        DetachUnchangedEntities();

        // Phase 11: OperationNotificationDisplayCsvLoader - 運行通知表示データの読み込み
        var operationNotificationDisplayCsvLoader = new OperationNotificationDisplayCsvLoader(loggerFactory.CreateLogger<OperationNotificationDisplayCsvLoader>());

        // Phase 12: RouteLockTrackCircuitCsvLoader - 進路鎖錠軌道回路データの読み込み
        var routeLockTrackCircuitCsvLoader = new RouteLockTrackCircuitCsvLoader(loggerFactory.CreateLogger<RouteLockTrackCircuitCsvLoader>());

        // Phase 13: TtcWindowCsvLoader - TTCウィンドウデータの読み込み
        var ttcWindowCsvLoader = new TtcWindowCsvLoader(loggerFactory.CreateLogger<TtcWindowCsvLoader>());

        // Phase 14: TtcWindowLinkCsvLoader - TTCウィンドウリンクデータの読み込み
        var ttcWindowLinkCsvLoader = new TtcWindowLinkCsvLoader(loggerFactory.CreateLogger<TtcWindowLinkCsvLoader>());

        // Phase 15: ThrowOutControlCsvLoader - 投出制御データの読み込み
        var throwOutControlCsvLoader = new ThrowOutControlCsvLoader(loggerFactory.CreateLogger<ThrowOutControlCsvLoader>());

        // Phase 16: OperationNotificationDisplayDbInitializer - 運行通知の初期化
        var operationNotificationInitializer = new OperationNotificationDisplayDbInitializer(
            context,
            datetimeRepository,
            loggerFactory.CreateLogger<OperationNotificationDisplayDbInitializer>(),
            operationNotificationDisplayCsvLoader);
        await operationNotificationInitializer.InitializeAsync(cancellationToken);

        // Phase 17: RouteDbInitializer - 進路の初期化
        var routeInitializer = new RouteDbInitializer(context, loggerFactory.CreateLogger<RouteDbInitializer>(), routeLockTrackCircuitCsvLoader);
        await routeInitializer.InitializeAsync(cancellationToken);

        // Phase 18: ServerStatusDbInitializer - サーバーステータスの初期化
        var serverStatusInitializer =
            new ServerStatusDbInitializer(context, loggerFactory.CreateLogger<ServerStatusDbInitializer>());
        await serverStatusInitializer.InitializeAsync(cancellationToken);

        // Phase 19: TtcDbInitializer - TTCの初期化
        var ttcInitializer = new TtcDbInitializer(context, loggerFactory.CreateLogger<TtcDbInitializer>(), ttcWindowCsvLoader, ttcWindowLinkCsvLoader);
        await ttcInitializer.InitializeAsync(cancellationToken);

        // Phase 20: ThrowOutControlDbInitializer - 投出制御の初期化
        var throwOutControlInitializer = new ThrowOutControlDbInitializer(
            context,
            loggerFactory.CreateLogger<ThrowOutControlDbInitializer>(),
            throwOutControlCsvLoader);
        await throwOutControlInitializer.InitializeAsync(cancellationToken);

        // Phase 21: SignalCsvLoader - 信号データの読み込み
        var signalLoader = new SignalCsvLoader(loggerFactory.CreateLogger<SignalCsvLoader>());
        var signalDataList = signalLoader.Load();

        // Phase 22: SignalDbInitializer - 信号と進路の初期化
        var signalInitializer = new SignalDbInitializer(context, loggerFactory.CreateLogger<SignalDbInitializer>());
        await signalInitializer.InitializeSignalsAsync(signalDataList, cancellationToken);
        await signalInitializer.InitializeNextSignalsAsync(signalDataList, cancellationToken);
        await signalInitializer.InitializeSignalRoutesAsync(signalDataList, cancellationToken);

        // Phase 23: TrackCircuitDbInitializer - 軌道回路信号の初期化
        await trackCircuitInitializer.InitializeTrackCircuitSignalsAsync(trackCircuitList, cancellationToken);

        DetachUnchangedEntities();

        // Phase 24: DbRendoTableInitializer - 鎖錠の初期化（駅ごと）
        foreach (var initializer in rendoTableInitializers)
        {
            await initializer.InitializeLocks();
        }

        // Phase 25: Finalize - 初期化の完了処理
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