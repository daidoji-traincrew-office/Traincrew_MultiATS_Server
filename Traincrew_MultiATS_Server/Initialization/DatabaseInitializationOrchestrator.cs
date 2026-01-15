using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Initialization.CsvLoaders;
using Traincrew_MultiATS_Server.Initialization.DbInitializers;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.Lock;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.LockConditionByRouteCentralControlLever;
using Traincrew_MultiATS_Server.Repositories.SwitchingMachineRoute;
using Traincrew_MultiATS_Server.Repositories.Transaction;

namespace Traincrew_MultiATS_Server.Initialization;

/// <summary>
///     Orchestrates the database initialization sequence
///     Coordinates CSV loading and database initialization in the correct order
///     Manages DI scopes for initialization
/// </summary>
public class DatabaseInitializationOrchestrator(
    ApplicationDbContext context,
    ILoggerFactory loggerFactory,
    IDateTimeRepository datetimeRepository,
    ILockConditionRepository lockConditionRepository,
    ILockRepository lockRepository,
    ILockConditionByRouteCentralControlLeverRepository lockConditionByRouteCentralControlLeverRepository,
    ISwitchingMachineRouteRepository switchingMachineRouteRepository,
    ITransactionRepository transactionRepository,
    InterlockingObjectDbInitializer interlockingObjectDbInitializer,
    SwitchingMachineRouteDbInitializer switchingMachineRouteDbInitializer,
    StationCsvLoader stationCsvLoader,
    TrackCircuitCsvLoader trackCircuitCsvLoader,
    SignalTypeCsvLoader signalTypeCsvLoader,
    TrainTypeCsvLoader trainTypeCsvLoader,
    TrainDiagramCsvLoader trainDiagramCsvLoader,
    RendoTableCsvLoader rendoTableCsvLoader,
    SignalCsvLoader signalCsvLoader,
    StationDbInitializer stationDbInitializer,
    TrackCircuitDbInitializer trackCircuitDbInitializer,
    SignalTypeDbInitializer signalTypeDbInitializer,
    TrainDbInitializer trainDbInitializer,
    OperationNotificationDisplayDbInitializer operationNotificationDisplayDbInitializer,
    RouteLockTrackCircuitDbInitializer routeLockTrackCircuitDbInitializer,
    ServerStatusDbInitializer serverStatusDbInitializer,
    TtcDbInitializer ttcDbInitializer,
    ThrowOutControlDbInitializer throwOutControlDbInitializer,
    SignalDbInitializer signalDbInitializer,
    ILogger<DatabaseInitializationOrchestrator> logger)
{
    /// <summary>
    ///     Execute the complete database initialization sequence
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting database initialization");

        // Phase 1: StationCsvLoader - 駅一覧の読み込み
        var stationList = await stationCsvLoader.LoadAsync(cancellationToken);

        // Phase 2: StationDbInitializer - 駅の初期化
        await stationDbInitializer.InitializeStationsAsync(stationList, cancellationToken);
        await stationDbInitializer.InitializeStationTimerStatesAsync(cancellationToken);

        // Phase 3: TrackCircuitCsvLoader - 軌道回路一覧の読み込み
        var trackCircuitList = await trackCircuitCsvLoader.LoadAsync(cancellationToken);

        // Phase 4: TrackCircuitDbInitializer - 軌道回路の初期化
        await trackCircuitDbInitializer.InitializeTrackCircuitsAsync(trackCircuitList, cancellationToken);

        // Phase 5: SignalTypeCsvLoader - 信号タイプ一覧の読み込み
        var signalTypeList = await signalTypeCsvLoader.LoadAsync(cancellationToken);

        // Phase 6: SignalTypeDbInitializer - 信号タイプの初期化
        await signalTypeDbInitializer.InitializeSignalTypesAsync(signalTypeList, cancellationToken);

        // Phase 7: TrainTypeCsvLoader - 列車タイプデータの読み込み
        var trainTypeList = trainTypeCsvLoader.Load();

        // Phase 8: TrainDiagramCsvLoader - 列車ダイヤデータの読み込み
        var trainDiagramList = trainDiagramCsvLoader.Load();

        // Phase 9: TrainDbInitializer - 列車データの初期化
        await trainDbInitializer.InitializeTrainTypesAsync(trainTypeList, cancellationToken);
        await trainDbInitializer.InitializeTrainDiagramsAsync(trainDiagramList, cancellationToken);

        // Phase 10: DbRendoTableInitializer - 連動表オブジェクトの初期化（駅ごと）
        var rendoTableInitializers = await CreateRendoTableInitializersAsync(cancellationToken);
        foreach (var initializer in rendoTableInitializers)
        {
            await initializer.InitializeObjects();
        }

        DetachUnchangedEntities();

        // Phase 16: OperationNotificationDisplayDbInitializer - 運行通知の初期化
        await operationNotificationDisplayDbInitializer.InitializeAsync(cancellationToken);

        // Phase 17: RouteLockTrackCircuitDbInitializer - 進路の初期化
        await routeLockTrackCircuitDbInitializer.InitializeAsync(cancellationToken);

        // Phase 18: ServerStatusDbInitializer - サーバーステータスの初期化
        await serverStatusDbInitializer.InitializeAsync(cancellationToken);

        // Phase 19: TtcDbInitializer - TTCの初期化
        await ttcDbInitializer.InitializeAsync(cancellationToken);

        // Phase 20: ThrowOutControlDbInitializer - 総括制御の初期化
        await throwOutControlDbInitializer.InitializeAsync(cancellationToken);

        // Phase 21: SignalCsvLoader - 信号データの読み込み
        var signalDataList = signalCsvLoader.Load();

        // Phase 22: SignalDbInitializer - 信号と進路の初期化
        await signalDbInitializer.InitializeSignalsAsync(signalDataList, cancellationToken);
        await signalDbInitializer.InitializeNextSignalsAsync(signalDataList, cancellationToken);
        await signalDbInitializer.InitializeSignalRoutesAsync(signalDataList, cancellationToken);

        // Phase 23: TrackCircuitDbInitializer - 軌道回路信号の初期化
        await trackCircuitDbInitializer.InitializeTrackCircuitSignalsAsync(trackCircuitList, cancellationToken);

        DetachUnchangedEntities();

        await using(var transaction = await transactionRepository.BeginTransactionAsync(cancellationToken)){
            // 常に最新化するため、既存のデータを削除する
            logger.LogInformation("Deleting existing lock-related data");
            await switchingMachineRouteRepository.DeleteAll();
            await lockConditionRepository.DeleteAll();
            await lockRepository.DeleteAll();
            await lockConditionByRouteCentralControlLeverRepository.DeleteAll();
            logger.LogInformation("Existing lock-related data deleted");

            // Phase 24: DbRendoTableInitializer - 鎖錠の初期化（駅ごと）
            foreach (var initializer in rendoTableInitializers)
            {
                await initializer.InitializeLocks();
            }
             // Phase 25: Finalize - 初期化の完了処理
            DetachUnchangedEntities();
            await FinalizeInitializationAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        logger.LogInformation("Database initialization completed");
    }

    private async Task<List<DbRendoTableInitializer>> CreateRendoTableInitializersAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating Rendo Table initializers");

        var rendoTableData = await rendoTableCsvLoader.LoadAllAsync(cancellationToken);

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

    private async Task FinalizeInitializationAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Finalizing initialization");

        await interlockingObjectDbInitializer.InitializeAsync(cancellationToken);
        await switchingMachineRouteDbInitializer.InitializeSwitchingMachineRoutesAsync(cancellationToken);
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