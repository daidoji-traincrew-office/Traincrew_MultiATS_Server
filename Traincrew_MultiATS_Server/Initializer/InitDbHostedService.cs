using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Scheduler;
using Traincrew_MultiATS_Server.Services;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Initializer;

public class InitDbHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ILoggerFactory loggerFactory
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var datetimeRepository = scope.ServiceProvider.GetRequiredService<IDateTimeRepository>();
        var lockConditionRepository = scope.ServiceProvider.GetRequiredService<ILockConditionRepository>();
        var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();
        var schedulerManager =  scope.ServiceProvider.GetRequiredService<SchedulerManager>();
        var dbInitializer = await CreateDBInitializer(context, lockConditionRepository, cancellationToken);
        if (dbInitializer != null)
        {
            await dbInitializer.Initialize();
        }

        await InitTrainTypes(context, cancellationToken);
        await InitTrainDiagrams(context, cancellationToken);
        var rendoTableInitializers =
            await CreateDbRendoTableInitializer(context, datetimeRepository, cancellationToken);
        foreach (var initializer in rendoTableInitializers)
        {
            await initializer.InitializeObjects();
        }

        DetachUnchangedEntities(context);
        await InitOperationNotificationDisplay(context, datetimeRepository, cancellationToken);
        await InitRouteCsv(context, cancellationToken);
        await InitServerStatus(context, cancellationToken);
        await InitTtcWindows(context, cancellationToken);
        await InitTtcWindowLinks(context, cancellationToken);

        if (dbInitializer != null)
        {
            await dbInitializer.InitializeAfterCreateRoute();
        }

        DetachUnchangedEntities(context);
        foreach (var initializer in rendoTableInitializers)
        {
            await initializer.InitializeLocks();
        }

        if (dbInitializer != null)
        {
            DetachUnchangedEntities(context);
            await dbInitializer.InitializeAfterCreateLockCondition();
        }

        schedulerManager.StartServerModeScheduler();
        await serverService.UpdateSchedulerAsync();
    }

    private async Task<DbInitializer?> CreateDBInitializer(ApplicationDbContext context,
        ILockConditionRepository lockConditionRepository,
        CancellationToken cancellationToken)
    {
        var jsonstring = await File.ReadAllTextAsync("./Data/DBBase.json", cancellationToken);
        var DBBase = JsonSerializer.Deserialize<DBBasejson>(jsonstring);
        var logger = loggerFactory.CreateLogger<DbInitializer>();
        return DBBase != null
            ? new DbInitializer(DBBase, context, lockConditionRepository, logger, cancellationToken)
            : null;
    }

    private async Task<List<DbRendoTableInitializer>> CreateDbRendoTableInitializer(
        ApplicationDbContext context,
        IDateTimeRepository dateTimeRepository,
        CancellationToken cancellationToken)
    {
        var rendoTableDir = new DirectoryInfo("./Data/RendoTable");
        if (!rendoTableDir.Exists)
        {
            return [];
        }

        var logger = loggerFactory.CreateLogger<DbRendoTableInitializer>();

        List<DbRendoTableInitializer> initializers = [];
        foreach (var file in rendoTableDir.EnumerateFiles())
        {
            if (file.Extension != ".csv")
            {
                continue;
            }

            var stationId = file.Name.Replace(".csv", "");
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };
            using var reader = new StreamReader(file.FullName);
            // ヘッダー行を読み飛ばす
            await reader.ReadLineAsync(cancellationToken);
            using var csv = new CsvReader(reader, config);
            var records = await csv
                .GetRecordsAsync<RendoTableCSV>(cancellationToken)
                .ToListAsync(cancellationToken);
            var initializer =
                new DbRendoTableInitializer(stationId, records, context, dateTimeRepository, logger, cancellationToken);
            initializers.Add(initializer);
        }

        return initializers;
    }

    private async Task InitOperationNotificationDisplay(
        ApplicationDbContext context,
        IDateTimeRepository dateTimeRepository,
        CancellationToken cancellationToken)
    {
        var file = new FileInfo("./Data/運転告知器.csv");
        if (!file.Exists)
        {
            return;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
        };
        using var reader = new StreamReader(file.FullName);
        // ヘッダー行を読み飛ばす
        await reader.ReadLineAsync(cancellationToken);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<OperationNotificationDisplayCsvMap>();
        var records = await csv
            .GetRecordsAsync<OperationNotificationDisplayCsv>(cancellationToken)
            .ToListAsync(cancellationToken);
        var trackCircuitNames = records
            .SelectMany(r => r.TrackCircuitNames)
            .ToList();
        var trackCircuits = await context.TrackCircuits
            .Where(tc => trackCircuitNames.Contains(tc.Name))
            .ToDictionaryAsync(tc => tc.Name, cancellationToken);
        var operationNotificationDisplayNames = await context.OperationNotificationDisplays
            .Select(ond => ond.Name)
            .ToListAsync(cancellationToken);
        List<TrackCircuit> changedTrackCircuits = [];
        foreach (var record in records)
        {
            var name = record.Name;
            if (operationNotificationDisplayNames.Contains(name))
            {
                continue;
            }

            context.OperationNotificationDisplays.Add(new()
            {
                Name = name,
                StationId = record.StationId,
                IsUp = record.IsUp,
                IsDown = record.IsDown,
                OperationNotificationState = new()
                {
                    DisplayName = name,
                    Type = OperationNotificationType.None,
                    Content = "",
                    OperatedAt = dateTimeRepository.GetNow().AddDays(-1)
                }
            });
            foreach (var trackCircuitName in record.TrackCircuitNames)
            {
                if (!trackCircuits.TryGetValue(trackCircuitName, out var trackCircuit))
                {
                    continue;
                }

                trackCircuit.OperationNotificationDisplayName = name;
                context.TrackCircuits.Update(trackCircuit);
                changedTrackCircuits.Add(trackCircuit);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var trackCircuit in changedTrackCircuits)
        {
            context.Entry(trackCircuit).State = EntityState.Detached;
        }
    }

    private async Task InitRouteCsv(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var file = new FileInfo("./Data/進路.csv");
        if (!file.Exists)
        {
            return;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
        };
        using var reader = new StreamReader(file.FullName);
        // ヘッダー行を読み飛ばす
        await reader.ReadLineAsync(cancellationToken);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<RouteLockTrackCircuitCsvMap>();
        var records = await csv
            .GetRecordsAsync<RouteLockTrackCircuitCsv>(cancellationToken)
            .ToListAsync(cancellationToken);
        var routes = await context.Routes
            .Select(r => new { r.Name, r.Id })
            .ToDictionaryAsync(r => r.Name, r => r.Id, cancellationToken);
        var trackCircuits = await context.TrackCircuits
            .Select(tc => new { tc.Name, tc.Id })
            .ToDictionaryAsync(tc => tc.Name, tc => tc.Id, cancellationToken);
        var routeLockTrackCircuits = (await context.RouteLockTrackCircuits
            .Select(r => new { r.RouteId, r.TrackCircuitId })
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var record in records)
        {
            // 該当進路が登録されていない場合スキップ
            if (!routes.TryGetValue(record.RouteName, out var routeId))
            {
                continue;
            }

            foreach (var trackCircuitName in record.TrackCircuitNames)
            {
                // 該当軌道回路が登録されていない場合スキップ
                if (!trackCircuits.TryGetValue(trackCircuitName, out var trackCircuitId))
                {
                    continue;
                }

                // 既に登録済みの場合、スキップ
                if (routeLockTrackCircuits.Contains(new { RouteId = routeId, TrackCircuitId = trackCircuitId }))
                {
                    continue;
                }

                context.RouteLockTrackCircuits.Add(new()
                {
                    RouteId = routeId,
                    TrackCircuitId = trackCircuitId
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitServerStatus(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var serverState = await context.ServerStates.FirstOrDefaultAsync(cancellationToken);
        if (serverState != null)
        {
            return;
        }

        context.ServerStates.Add(new()
        {
            Mode = ServerMode.Off
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitTtcWindows(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var file = new FileInfo("./Data/TTC列番窓.csv");
        if (!file.Exists)
        {
            return;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
        };
        using var reader = new StreamReader(file.FullName);
        // ヘッダー行を読み飛ばす
        await reader.ReadLineAsync(cancellationToken);

        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<TtcWindowCsvMap>();
        var records = await csv
            .GetRecordsAsync<TtcWindowCsv>(cancellationToken)
            .ToListAsync(cancellationToken);

        var existingWindows = await context.TtcWindows
            .Select(w => w.Name)
            .AsAsyncEnumerable()
            .ToHashSetAsync(cancellationToken);
        var trackCircuitIdByName = await context.TrackCircuits
            .ToDictionaryAsync(tc => tc.Name, tc => tc.Id, cancellationToken);

        foreach (var record in records)
        {
            if (existingWindows.Contains(record.Name))
            {
                continue;
            }

            context.TtcWindows.Add(new()
            {
                Name = record.Name,
                StationId = record.StationId,
                Type = record.Type,
                TtcWindowState = new()
                {
                    TrainNumber = ""
                }
            });

            foreach (var displayStation in record.DisplayStations)
            {
                context.TtcWindowDisplayStations.Add(new()
                {
                    TtcWindowName = record.Name,
                    StationId = displayStation
                });
            }

            foreach (var trackCircuit in record.TrackCircuits)
            {
                context.TtcWindowTrackCircuits.Add(new()
                {
                    TtcWindowName = record.Name,
                    TrackCircuitId = trackCircuitIdByName[trackCircuit]
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitTtcWindowLinks(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var file = new FileInfo("./Data/TTC列番窓リンク設定.csv");
        if (!file.Exists)
        {
            return;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
        };
        using var reader = new StreamReader(file.FullName);
        // ヘッダー行を読み飛ばす
        await reader.ReadLineAsync(cancellationToken);

        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<TtcWindowLinkCsvMap>();
        var records = await csv
            .GetRecordsAsync<TtcWindowLinkCsv>(cancellationToken).ToListAsync(cancellationToken);

        var existingLinks = await context.TtcWindowLinks
            .Select(l => new { Source = l.SourceTtcWindowName, Target = l.TargetTtcWindowName })
            .AsAsyncEnumerable()
            .ToHashSetAsync(cancellationToken);
        var trackCircuitIdByName = await context.TrackCircuits
            .ToDictionaryAsync(tc => tc.Name, tc => tc.Id, cancellationToken);
        var routeIdByName = await context.Routes
            .ToDictionaryAsync(r => r.Name, r => r.Id, cancellationToken);

        foreach (var record in records)
        {
            if (existingLinks.Contains(new { record.Source, record.Target }))
            {
                continue;
            }

            var ttcWindowLink = new TtcWindowLink
            {
                SourceTtcWindowName = record.Source,
                TargetTtcWindowName = record.Target,
                Type = record.Type,
                IsEmptySending = record.IsEmptySending,
                TrackCircuitCondition = record.TrackCircuitCondition != null
                    ? trackCircuitIdByName[record.TrackCircuitCondition]
                    : null
            };
            context.TtcWindowLinks.Add(ttcWindowLink);

            foreach (var routeCondition in record.RouteConditions)
            {
                if (!routeIdByName.TryGetValue(routeCondition, out var routeId))
                {
                    continue;
                }

                context.TtcWindowLinkRouteConditions.Add(new()
                {
                    RouteId = routeId,
                    TtcWindowLink = ttcWindowLink
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 列車種別(train_type)をCSVから初期化
    /// </summary>
    private async Task InitTrainTypes(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var file = new FileInfo("./Data/種別.csv");
        if (!file.Exists)
        {
            return;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };
        using var reader = new StreamReader(file.FullName);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<TrainTypeCsvMap>();
        var records = csv.GetRecords<TrainTypeCsv>().ToList();

        var existingIds = await context.TrainTypes.Select(t => t.Id).ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            if (existingIds.Contains(record.Id))
            {
                continue;
            }

            context.TrainTypes.Add(new()
            {
                Id = record.Id,
                Name = record.Name
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 列車ダイヤ(train_diagram)をCSVから初期化
    /// </summary>
    private async Task InitTrainDiagrams(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var file = new FileInfo("./Data/列車.csv");
        if (!file.Exists)
        {
            return;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };
        using var reader = new StreamReader(file.FullName);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<TrainDiagramCsvMap>();
        var records = csv.GetRecords<TrainDiagramCsv>().ToList();

        var existingNumbers = await context.TrainDiagrams.Select(t => t.TrainNumber).ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            if (existingNumbers.Contains(record.TrainNumber))
            {
                continue;
            }

            context.TrainDiagrams.Add(new()
            {
                TrainNumber = record.TrainNumber,
                TrainTypeId = record.TypeId,
                FromStationId = record.FromStationId,
                ToStationId = record.ToStationId,
                DiaId = record.DiaId
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// UnchangedなEntityをすべてDetachする。
    /// </summary>
    /// <param name="context">DbContext</param>
    private static void DetachUnchangedEntities(ApplicationDbContext context)
    {
        var changedEntriesCopy = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Unchanged)
            .ToList();

        foreach (var entry in changedEntriesCopy)
        {
            entry.State = EntityState.Detached;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var schedulerManager = serviceScopeFactory.CreateScope()
            .ServiceProvider.GetRequiredService<SchedulerManager>();
        // Stop all schedulers
        await schedulerManager.Stop();
        await schedulerManager.StopServerModeScheduler();
    }
}
