using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Scheduler;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.HostedService;

public class InitDbHostedService(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    private readonly List<Scheduler.Scheduler> _schedulers = [];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var datetimeRepository = scope.ServiceProvider.GetRequiredService<IDateTimeRepository>();
        var dbInitializer = await CreateDBInitializer(context, cancellationToken);
        if (dbInitializer != null)
        {
            await dbInitializer.Initialize();
        }

        await InitRendoTable(context, datetimeRepository, cancellationToken);
        if (dbInitializer != null)
        {
            await dbInitializer.InitializeSignalRoute();
        }

        _schedulers.AddRange([
            new SwitchingMachineScheduler(serviceScopeFactory),
            new RendoScheduler(serviceScopeFactory),
        ]);
    }

    private async Task<DbInitializer?> CreateDBInitializer(ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var jsonstring = await File.ReadAllTextAsync("./Data/DBBase.json", cancellationToken);
        var DBBase = JsonSerializer.Deserialize<DBBasejson>(jsonstring);
        return DBBase != null ? new DbInitializer(DBBase, context, cancellationToken) : null;
    }

    private async Task InitRendoTable(
        ApplicationDbContext context,
        IDateTimeRepository dateTimeRepository,
        CancellationToken cancellationToken)
    {
        var rendoTableDir = new DirectoryInfo("./Data/RendoTable");
        if (!rendoTableDir.Exists)
        {
            return;
        }

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
                new DbRendoTableInitializer(stationId, records, context, dateTimeRepository, cancellationToken);
            initializers.Add(initializer);
            await initializer.InitializeObjects();
        }

        var changedEntriesCopy = context.ChangeTracker.Entries()
            .Where(e => e.State is
                EntityState.Added or EntityState.Modified or EntityState.Deleted or EntityState.Unchanged)
            .ToList();

        foreach (var entry in changedEntriesCopy)
        {
            entry.State = EntityState.Detached;
        }

        foreach (var initializer in initializers)
        {
            await initializer.InitializeLocks();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_schedulers.Select(s => s.Stop()));
    }
}

internal class DbInitializer(DBBasejson DBBase, ApplicationDbContext context, CancellationToken cancellationToken)
{
    internal async Task Initialize()
    {
        await InitStation();
        await InitTrackCircuit();
        await InitSignalType();
        await InitSignal();
        await InitNextSignal();
        await InitTrackCircuitSignal();
    }

    internal async Task InitializeSignalRoute()
    {
        var signalRoutes = await context.SignalRoutes
            .Include(sr => sr.Route)
            .ToListAsync(cancellationToken);
        var routes = await context.Routes
            .ToDictionaryAsync(r => r.Name, cancellationToken);
        foreach (var signal in DBBase.signalDataList)
        {
            foreach (var routeName in signal.RouteNames)
            {
                // Todo: FW 全探索なので改善したほうがいいかも
                if (signalRoutes.Any(sr => sr.SignalName == signal.Name && sr.Route.Name == routeName))
                {
                    continue;
                }

                if (!routes.TryGetValue(routeName, out var route))
                {
                    // Todo: 例外を出す
                    continue;
                }

                context.SignalRoutes.Add(new()
                {
                    SignalName = signal.Name,
                    RouteId = route.Id
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitStation()
    {
        var stationNames = (await context.Stations
            .Select(s => s.Name)
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var station in DBBase.stationList)
        {
            if (stationNames.Contains(station.Name))
            {
                continue;
            }

            context.Stations.Add(new()
            {
                Id = station.Id,
                Name = station.Name,
                IsStation = station.IsStation,
                IsPassengerStation = station.IsPassengerStation
            });
        }
    }

    private async Task InitTrackCircuit()
    {
        // 全軌道回路情報を取得
        var trackCircuitNames = (await context.TrackCircuits
            .Select(tc => tc.Name)
            .ToListAsync(cancellationToken)).ToHashSet();

        foreach (var item in DBBase.trackCircuitList)
        {
            if (trackCircuitNames.Contains(item.Name))
            {
                continue;
            }

            context.TrackCircuits.Add(new()
            {
                // Todo: ProtectionZoneの未定義部分がなくなったら、ProtectionZoneのデフォルト値の設定を解除
                ProtectionZone = item.ProtectionZone ?? 99,
                Name = item.Name,
                Type = ObjectType.TrackCircuit,
                TrackCircuitState = new()
                {
                    IsShortCircuit = false,
                    IsLocked = false,
                    TrainNumber = ""
                }
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitSignal()
    {
        // 軌道回路情報を取得
        var trackCircuits = await context.TrackCircuits
            .Select(tc => new { tc.Id, tc.Name })
            .ToDictionaryAsync(tc => tc.Name, tc => tc.Id, cancellationToken);
        // 既に登録済みの信号情報を取得
        var signalNames = (await context.Signals
            .Select(s => s.Name)
            .ToListAsync(cancellationToken)).ToHashSet();
        // 駅マスタを取得
        var stations = await context.Stations
            .ToListAsync(cancellationToken);
        // 信号情報登録
        foreach (var signalData in DBBase.signalDataList)
        {
            // 既に登録済みの場合、スキップ
            if (signalNames.Contains(signalData.Name))
            {
                continue;
            }

            ulong trackCircuitId = 0;
            if (signalData.Name.StartsWith("上り閉塞") || signalData.Name.StartsWith("下り閉塞"))
            {
                var trackCircuitName = $"{signalData.Name.Replace("閉塞", "")}T";
                trackCircuits.TryGetValue(trackCircuitName, out trackCircuitId);
            }

            var stationId = stations
                .Where(s => signalData.Name.StartsWith(s.Name))
                .Select(s => s.Id)
                .FirstOrDefault();

            context.Signals.Add(new()
            {
                Name = signalData.Name,
                StationId = stationId,
                TrackCircuitId = trackCircuitId > 0 ? trackCircuitId : null,
                TypeName = signalData.TypeName,
                SignalState = new()
                {
                    IsLighted = true,
                }
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitSignalType()
    {
        var signalTypeNames = (await context.SignalTypes
            .Select(st => st.Name)
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var signalTypeData in DBBase.signalTypeList)
        {
            if (signalTypeNames.Contains(signalTypeData.Name))
            {
                continue;
            }

            context.SignalTypes.Add(new()
            {
                Name = signalTypeData.Name,
                RIndication = GetSignalIndication(signalTypeData.RIndication),
                YYIndication = GetSignalIndication(signalTypeData.YYIndication),
                YIndication = GetSignalIndication(signalTypeData.YIndication),
                YGIndication = GetSignalIndication(signalTypeData.YGIndication),
                GIndication = GetSignalIndication(signalTypeData.GIndication)
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static SignalIndication GetSignalIndication(string indication)
    {
        return indication switch
        {
            "R" => SignalIndication.R,
            "YY" => SignalIndication.YY,
            "Y" => SignalIndication.Y,
            "YG" => SignalIndication.YG,
            "G" => SignalIndication.G,
            _ => SignalIndication.R
        };
    }

    private async Task InitNextSignal()
    {
        const int maxDepth = 4;
        foreach (var signalData in DBBase.signalDataList)
        {
            var nextSignalNames = signalData.NextSignalNames ?? [];
            foreach (var nextSignalName in nextSignalNames)
            {
                // Todo: ここでN+1問題が発生しているので、改善したほうが良いかも
                // 既に登録済みの場合、スキップ
                if (context.NextSignals.Any(ns =>
                        ns.SignalName == signalData.Name && ns.TargetSignalName == nextSignalName))
                {
                    continue;
                }

                context.NextSignals.Add(new()
                {
                    SignalName = signalData.Name,
                    SourceSignalName = signalData.Name,
                    TargetSignalName = nextSignalName,
                    Depth = 1
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var allSignals = await context.Signals.ToListAsync(cancellationToken);
        var nextSignalList = await context.NextSignals
            .Where(ns => ns.Depth == 1)
            .GroupBy(ns => ns.SignalName)
            .ToListAsync(cancellationToken);
        var nextSignalDict = nextSignalList
            .ToDictionary(
                g => g.Key,
                g => g.Select(ns => ns.TargetSignalName).ToList()
            );
        // Todo: このロジック、絶対テスト書いたほうがいい(若干複雑な処理をしてしまったので)
        for (var depth = 2; depth <= maxDepth; depth++)
        {
            var nextNextSignalDict = await context.NextSignals
                .Where(ns => ns.Depth == depth - 1)
                .GroupBy(ns => ns.SignalName)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(ns => ns.TargetSignalName).ToList(),
                    cancellationToken
                );
            List<NextSignal> nextNextSignals = [];
            // 全信号機でループ
            foreach (var signal in allSignals)
            {
                // 次信号機がない場合はスキップ
                if (!nextNextSignalDict.TryGetValue(signal.Name, out var nextSignals))
                {
                    continue;
                }

                foreach (var nextSignal in nextSignals)
                {
                    // 次信号機の次信号機を取ってくる
                    if (!nextSignalDict.TryGetValue(nextSignal, out var nnSignals))
                    {
                        continue;
                    }

                    foreach (var nextNextSignal in nnSignals)
                    {
                        // Todo: N+1問題が発生しているので、改善したほうが良いかも
                        if (context.NextSignals.Any(ns =>
                                ns.SignalName == signal.Name && ns.TargetSignalName == nextNextSignal))
                        {
                            continue;
                        }

                        context.NextSignals.Add(new()
                        {
                            SignalName = signal.Name,
                            SourceSignalName = nextSignal,
                            TargetSignalName = nextNextSignal,
                            Depth = depth
                        });
                        await context.SaveChangesAsync(cancellationToken);
                    }
                }
            }
        }
    }

    private async Task InitTrackCircuitSignal()
    {
        foreach (var trackCircuit in DBBase.trackCircuitList)
        {
            var trackCircuitEntity = await context.TrackCircuits
                .FirstOrDefaultAsync(tc => tc.Name == trackCircuit.Name, cancellationToken);

            if (trackCircuitEntity == null) continue;
            foreach (var signalName in trackCircuit.NextSignalNamesUp ?? [])
            {
                // Todo: ここでN+1問題が発生しているので、改善したほうが良いかも
                if (context.TrackCircuitSignals.Any(tcs =>
                        tcs.TrackCircuitId == trackCircuitEntity.Id && tcs.SignalName == signalName && tcs.IsUp))
                {
                    continue;
                }

                context.TrackCircuitSignals.Add(new()
                {
                    TrackCircuitId = trackCircuitEntity.Id,
                    SignalName = signalName,
                    IsUp = true
                });
            }

            foreach (var signalName in trackCircuit.NextSignalNamesDown ?? [])
            {
                // Todo: ここでN+1問題が発生しているので、改善したほうが良いかも
                if (context.TrackCircuitSignals.Any(tcs =>
                        tcs.TrackCircuitId == trackCircuitEntity.Id && tcs.SignalName == signalName && !tcs.IsUp))
                {
                    continue;
                }

                context.TrackCircuitSignals.Add(new()
                {
                    TrackCircuitId = trackCircuitEntity.Id,
                    SignalName = signalName,
                    IsUp = false
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

public partial class DbRendoTableInitializer
{
    const string NameSwitchingMachine = "転てつ器";
    private const string NameAnd = "and";
    private const string NameOr = "or";

    private static readonly Dictionary<string, List<string>> StationIdMap = new()
    {
        // 大道寺: 江ノ原検車区、藤江
        { "TH65", ["TH66S", "TH64"] },
        // 江ノ原検車区: 大道寺
        { "TH66S", ["TH65"] },
        // 浜園: 津崎
        { "TH70", ["TH71"] },
        // 津崎: 浜園
        { "TH71", ["TH70"] }
    };

    [GeneratedRegex(@"\d+")]
    private static partial Regex RegexIntParse();

    // てこ名を抽出するための正規表現
    [GeneratedRegex(@"(\d+)(?:R|L)(Z?)")]
    private static partial Regex RegexLeverParse();

    // 信号制御欄から統括制御とそれ以外の部位に分けるための正規表現
    [GeneratedRegex(@"^(.*?)(?:\(\(([^\)\s]+)\)\)\s*)*$")]
    private static partial Regex RegexSignalControl();

    // 連動図表の鎖錠欄の諸々のトークンを抽出するための正規表現
    [GeneratedRegex(@"\[\[|\]\]|\(\(|\)\)|\[|\]|\{|\}|\(|\)|但|又は|[A-Z\dｲﾛ秒]+")]
    private static partial Regex TokenRegex();

    // ReSharper disable InconsistentNaming
    private readonly string stationId;
    private readonly List<RendoTableCSV> rendoTableCsvs;
    private readonly ApplicationDbContext context;
    private readonly IDateTimeRepository dateTimeRepository;
    private readonly CancellationToken cancellationToken;
    private readonly List<string> otherStations;
    // ReSharper restore InconsistentNaming

    public DbRendoTableInitializer(
        string stationId,
        List<RendoTableCSV> rendoTableCsvs,
        ApplicationDbContext context,
        IDateTimeRepository dateTimeRepository,
        CancellationToken cancellationToken)
    {
        this.stationId = stationId;
        this.rendoTableCsvs = rendoTableCsvs;
        this.context = context;
        this.dateTimeRepository = dateTimeRepository;
        this.cancellationToken = cancellationToken;
        otherStations = StationIdMap.GetValueOrDefault(stationId) ?? [];
    }

    internal async Task InitializeObjects()
    {
        PreprocessCsv();
        await InitLever();
        await InitDestinationButtons();
        await InitRoutes();
    }

    internal async Task InitializeLocks()
    {
        // PreprocessCsv();
        await InitLocks();
    }

    private void PreprocessCsv()
    {
        // ヒューマンリーダブルな空白や同上などの項目を補完し、後続の処理で扱いやすくする
        var oldName = "";
        var previousStart = "";
        var preivousLockTime = "";
        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            if (string.IsNullOrWhiteSpace(rendoTableCsv.Name) || rendoTableCsv.Name.StartsWith('同'))
            {
                rendoTableCsv.Name = oldName;
            }
            else
            {
                oldName = rendoTableCsv.Name;
            }

            if (string.IsNullOrWhiteSpace(rendoTableCsv.Start))
            {
                rendoTableCsv.Start = previousStart;
            }

            if (previousStart != rendoTableCsv.Start)
            {
                preivousLockTime = rendoTableCsv.ApproachTime;
            }
            else
            {
                rendoTableCsv.ApproachTime = preivousLockTime;
            }

            previousStart = rendoTableCsv.Start;
        }
    }

    private async Task InitLever()
    {
        // 該当駅の全てのてこを取得
        var leverNames = (await context.Levers
            .Where(l => l.Name.StartsWith(stationId))
            .Select(l => l.Name)
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            LeverType leverType;
            if (rendoTableCsv.Name.EndsWith("信号機"))
            {
                leverType = LeverType.Route;
            }
            else if (rendoTableCsv.Name.StartsWith(NameSwitchingMachine))
            {
                leverType = LeverType.SwitchingMachine;
            }
            else
            {
                continue;
            }

            // 転てつ器の場合、転てつ器を登録
            SwitchingMachine? switchingMachine = null;
            if (leverType == LeverType.SwitchingMachine)
            {
                switchingMachine = new()
                {
                    Name = CalcSwitchingMachineName(rendoTableCsv.Start, stationId),
                    TcName = "",
                    Type = ObjectType.SwitchingMachine,
                    SwitchingMachineState = new()
                    {
                        IsSwitching = false,
                        IsReverse = NR.Normal,
                        SwitchEndTime = dateTimeRepository.GetNow().AddDays(-1)
                    }
                };
            }

            // てこを登録
            var name = CalcLeverName(rendoTableCsv.Start, stationId);
            if (rendoTableCsv.Start.Length <= 0 || leverNames.Contains(name))
            {
                continue;
            }

            context.Levers.Add(new()
            {
                Name = name,
                Type = ObjectType.Lever,
                LeverType = leverType,
                SwitchingMachine = switchingMachine,
                LeverState = new()
                {
                    IsReversed = LCR.Center
                }
            });
            leverNames.Add(name);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitDestinationButtons()
    {
        // 既存の着点ボタン名を一括取得
        var existingButtonNames = await context.DestinationButtons
            .Select(db => db.Name)
            .ToListAsync(cancellationToken);

        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            if (string.IsNullOrWhiteSpace(rendoTableCsv.End) || rendoTableCsv.End is "L" or "R")
            {
                continue;
            }

            // ボタン名を生成
            var buttonName = CalcButtonName(rendoTableCsv.End, stationId);
            if (existingButtonNames.Contains(buttonName))
            {
                continue;
            }

            existingButtonNames.Add(buttonName);

            // 着点ボタンを追加
            context.DestinationButtons.Add(new()
            {
                Name = buttonName,
                StationId = stationId,
                DestinationButtonState = new()
                {
                    IsRaised = RaiseDrop.Drop,
                    OperatedAt = dateTimeRepository.GetNow().AddDays(-1)
                }
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitRoutes()
    {
        // 既存の進路名を一括取得
        var existingRouteNames = await context.Routes
            .Select(r => r.Name)
            .Where(r => r.StartsWith(stationId))
            .ToListAsync(cancellationToken);
        var leverDictionary = await context.Levers
            .Where(l => l.Name.StartsWith(stationId))
            .ToDictionaryAsync(l => l.Name, cancellationToken);

        List<(Route, ulong, string)> routes = [];
        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            // RouteTypeを決定
            RouteType routeType;
            if (rendoTableCsv.Name.Contains("場内"))
            {
                routeType = RouteType.Arriving;
            }
            else if (rendoTableCsv.Name.Contains("出発"))
            {
                routeType = RouteType.Departure;
            }
            else if (rendoTableCsv.Name.Contains("誘導"))
            {
                routeType = RouteType.Guide;
            }
            else if (rendoTableCsv.Name.Contains("入換信号"))
            {
                routeType = RouteType.SwitchSignal;
            }
            else if (rendoTableCsv.Name.Contains("入換標識"))
            {
                routeType = RouteType.SwitchRoute;
            }
            else
            {
                continue;
            }

            // 進路名を生成
            var routeName = CalcRouteName(rendoTableCsv.Start, rendoTableCsv.End, stationId);

            if (existingRouteNames.Contains(routeName))
            {
                continue;
            }

            existingRouteNames.Add(routeName);

            // 接近鎖状時素決定
            var matches = RegexIntParse().Match(rendoTableCsv.ApproachTime);
            int? approachLockTime = matches.Success ? int.Parse(matches.Value) : null;

            // 進路を追加
            Route route = new()
            {
                Name = routeName,
                TcName = "",
                RouteType = routeType,
                RootId = null,
                Indicator = "",
                ApproachLockTime = approachLockTime,
                RouteState = new()
                {
                    IsLeverRelayRaised = RaiseDrop.Drop,
                    IsRouteRelayRaised = RaiseDrop.Drop,
                    IsSignalControlRaised = RaiseDrop.Drop,
                    IsApproachLockRaised = RaiseDrop.Drop,
                    IsRouteLockRaised = RaiseDrop.Drop
                }
            };
            var leverName = CalcLeverName(rendoTableCsv.Start, stationId);
            var buttonName = CalcButtonName(rendoTableCsv.End, stationId);
            if (!leverDictionary.TryGetValue(leverName, out var lever))
            {
                continue;
            }

            routes.Add((route, lever.Id, buttonName));
            context.Routes.Add(route);
        }

        await context.SaveChangesAsync(cancellationToken);

        // 進路とてこと着点ボタンの関連付けを追加
        foreach (var (route, leverId, buttonName) in routes)
        {
            // Todo: 単線区間の進路は、着点がないことに注意
            context.RouteLeverDestinationButtons.Add(new()
            {
                RouteId = route.Id,
                LeverId = leverId,
                DestinationButtonName = buttonName
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitLocks()
    {
        // 必要なオブジェクトを取得
        // Todo: 接近鎖錠用に向けたオブジェクト取得
        var interlockingObjects = await context.InterlockingObjects
            .Where(io => io.Name.StartsWith(stationId) || otherStations.Any(s => io.Name.StartsWith(s)))
            .ToListAsync(cancellationToken);
        var locks = (await context.Locks
                .Select(l => l.ObjectId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        // 進路のDict
        var routes = interlockingObjects
            .OfType<Route>()
            .ToList();
        var routesByName = routes
            .ToDictionary(r => r.Name, r => r);
        var routesById = routes
            .ToDictionary(r => r.Id, r => r);
        // 転てつ器のDict
        var switchingMachines = interlockingObjects
            .OfType<SwitchingMachine>()
            .ToDictionary(sm => sm.Name, sm => sm);
        // その他のオブジェクトのDict
        var otherObjects = interlockingObjects
            .Where(io => io is not SwitchingMachine)
            .ToDictionary(io => io.Name, io => io);
        // てこ->進路へのDict
        var leverToRoute = await context.RouteLeverDestinationButtons
            .Join(
                context.Levers,
                rldb => rldb.LeverId,
                l => l.Id,
                (rr, l) => new { l.Name, rr.RouteId }
            )
            .GroupBy(x => x.Name)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => x.RouteId).ToList(),
                cancellationToken
            );
        var searchSwitchingMachine = new Func<LockItem, Task<List<InterlockingObject>>>(
            item =>
            {
                // Todo: もっときれいに書けるはず 
                var targetObject =
                    switchingMachines.GetValueOrDefault(CalcSwitchingMachineName(item.Name, item.StationId));
                List<InterlockingObject> result;
                if (targetObject != null)
                {
                    result = [targetObject];
                }
                else
                {
                    result = [];
                }

                return Task.FromResult(result);
            });
        var searchOtherObjects = new Func<LockItem, Task<List<InterlockingObject>>>(item =>
        {
            // 進路(単一) or 軌道回路の場合はこちら
            var key = ConvertHalfWidthToFullWidth(CalcRouteName(item.Name, "", item.StationId));
            var value = otherObjects.GetValueOrDefault(key);
            if (value != null)
            {
                return Task.FromResult<List<InterlockingObject>>([value]);
            }

            // 進路(複数)の場合
            var match = RegexLeverParse().Match(item.Name);
            if (match.Success)
            {
                var leverName = CalcLeverName(match.Groups[1].Value + match.Groups[2].Value, item.StationId);
                var routeIds = leverToRoute.GetValueOrDefault(leverName);
                if (routeIds != null)
                {
                    return Task.FromResult(routeIds.Select(InterlockingObject (r) => routesById[r]).ToList());
                }
            }

            return Task.FromResult<List<InterlockingObject>>([]);
        });

        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            var routeName = CalcRouteName(rendoTableCsv.Start, rendoTableCsv.End, stationId);
            var route = routesByName.GetValueOrDefault(routeName);
            if (route == null)
            {
                // Todo: Warningを出す
                continue;
            }

            // 既に何らか登録済みの場合、Continue
            if (locks.Contains(route.Id))
            {
                continue;
            }

            // Todo: CTC進路の場合と、その他の進路の場合で処理を分ける
            // 鎖錠欄(転てつ器)
            await RegisterLocks(
                rendoTableCsv.LockToSwitchingMachine, route.Id, searchSwitchingMachine, LockType.Lock, true);
            // 鎖錠欄(そのほか)
            await RegisterLocks(rendoTableCsv.LockToRoute, route.Id, searchOtherObjects, LockType.Lock);

            // 信号制御欄
            var matchSignalControl = RegexSignalControl().Match(rendoTableCsv.SignalControl);
            await RegisterLocks(matchSignalControl.Groups[1].Value, route.Id, searchOtherObjects,
                LockType.SignalControl);
            // 統括制御
            foreach (Capture capture in matchSignalControl.Groups[2].Captures)
            {
                var rootRoute = routesByName.GetValueOrDefault(CalcRouteName(capture.Value, "", stationId));
                if (rootRoute == null)
                {
                    // Todo: 例外を出す
                    continue;
                }

                route.RootId = rootRoute.Id;
                context.Routes.Update(route);
            }
            // Todo: 進路鎖錠
            // Todo: 接近鎖錠
        }

        // 転てつ器のてっ査鎖錠を処理する
        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            var targetSwitchingMachines = await searchSwitchingMachine(new()
            {
                Name = rendoTableCsv.Start,
                StationId = stationId
            });
            if (targetSwitchingMachines.Count == 0)
            {
                // Todo: 例外を出す
                continue;
            }

            var switchingMachine = targetSwitchingMachines[0];

            // 既に何らか登録済みの場合、Continue
            if (locks.Contains(switchingMachine.Id))
            {
                continue;
            }

            await RegisterLocks(rendoTableCsv.SignalControl, switchingMachine.Id, searchOtherObjects,
                LockType.Detector);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task RegisterLocks(string lockString, ulong objectId,
        Func<LockItem, Task<List<InterlockingObject>>> searchTargetObjects,
        LockType lockType,
        bool registerSwitchingMachineRoute = false)
    {
        var lockItems = CalcLockItems(lockString);
        foreach (var lockItem in lockItems)
        {
            Lock lockObject = new()
            {
                ObjectId = objectId,
                Type = lockType
            };
            context.Locks.Add(lockObject);
            LockCondition? root = null;
            // And/Or条件の処理
            if (lockItem.Name == NameOr)
            {
                root = new()
                {
                    Lock = lockObject,
                    Type = LockConditionType.Or
                };
            }

            if (lockItem.Name == NameAnd)
            {
                root = new()
                {
                    Lock = lockObject,
                    Type = LockConditionType.And
                };
            }

            if (root != null)
            {
                context.LockConditions.Add(root);
                foreach (var child in lockItem.Children)
                {
                    await RegisterLocksInner(child, root, searchTargetObjects);
                }

                continue;
            }

            // 単一オブジェクトに対する処理
            var targetObjects = await searchTargetObjects(lockItem);
            if (targetObjects.Count == 0)
            {
                // Todo: 例外を出す
                continue;
            }

            foreach (var targetObject in targetObjects)
            {
                context.LockConditionObjects.Add(new()
                {
                    Lock = lockObject,
                    ObjectId = targetObject.Id,
                    IsReverse = lockItem.IsReverse,
                    Type = LockConditionType.Object
                });
                if (!registerSwitchingMachineRoute)
                {
                    continue;
                }

                var switchingMachineRoute = new SwitchingMachineRoute
                {
                    SwitchingMachineId = targetObject.Id,
                    RouteId = objectId,
                    IsReverse = lockItem.IsReverse
                };
                context.SwitchingMachineRoutes.Add(switchingMachineRoute);
            }
        }
    }

    private async Task RegisterLocksInner(LockItem item, LockCondition parent,
        Func<LockItem, Task<List<InterlockingObject>>> searchTargetObjects)
    {
        LockCondition? current = null;
        if (item.Name == NameOr)
        {
            current = new()
            {
                Lock = parent.Lock,
                Type = LockConditionType.Or,
                Parent = parent
            };
        }

        if (item.Name == NameAnd)
        {
            current = new()
            {
                Lock = parent.Lock,
                Type = LockConditionType.And,
                Parent = parent
            };
        }

        if (current != null)
        {
            context.LockConditions.Add(current);
            foreach (var child in item.Children)
            {
                await RegisterLocksInner(child, current, searchTargetObjects);
            }

            return;
        }

        // 単一オブジェクトに対する処理
        var targetObjects = await searchTargetObjects(item);
        if (targetObjects.Count == 0)
        {
            throw new InvalidOperationException("対象のオブジェクトが見つかりません");
        }

        if (targetObjects.Count == 1)
        {
            context.LockConditionObjects.Add(new()
            {
                Lock = parent.Lock,
                ObjectId = targetObjects[0].Id,
                Parent = parent,
                IsReverse = item.IsReverse,
                Type = LockConditionType.Object
            });
            return;
        }

        current = new()
        {
            Lock = parent.Lock,
            Type = LockConditionType.Or,
            Parent = parent
        };
        foreach (var targetObject in targetObjects)
        {
            context.LockConditionObjects.Add(new()
            {
                Lock = current.Lock,
                Parent = current,
                ObjectId = targetObject.Id,
                IsReverse = item.IsReverse,
                Type = LockConditionType.Object
            });
        }
    }

    public List<LockItem> CalcLockItems(string lockString)
    {
        var tokens = TokenRegex().Matches(lockString)
            .Select(m => m.Value)
            .ToList();
        var enumerator = tokens.GetEnumerator();
        enumerator.MoveNext();
        return ParseToken(ref enumerator, stationId, false, false);
    }

    private List<LockItem> ParseToken(ref List<string>.Enumerator enumerator, string stationId, bool isReverse,
        bool isTotalControl)
    {
        List<LockItem> result = [];
        // なぜかCanBeNullなはずなのにそれを無視してしまうので
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        while (enumerator.Current != null)
        {
            var token = enumerator.Current;
            // 括弧とじならbreakし、再起元に判断を委ねる
            if (token is ")" or "]" or "]]" or "}")
            {
                break;
            }

            LockItem item;
            if (token == "{")
            {
                enumerator.MoveNext();
                var child = ParseToken(ref enumerator, stationId, isReverse, isTotalControl);
                if (enumerator.Current != "}")
                {
                    throw new InvalidOperationException("}が閉じられていません");
                }

                enumerator.MoveNext();
                result.AddRange(child);
            }
            else if (token == "((")
            {
                // 統括制御の処理を追加
                enumerator.MoveNext();
                var child = ParseToken(ref enumerator, stationId, isReverse, true);
                if (enumerator.Current != "))")
                {
                    throw new InvalidOperationException("))が閉じられていません");
                }

                enumerator.MoveNext();
                result.AddRange(child);
            }
            else if (token.StartsWith('['))
            {
                var count = token.Length;
                var targetStationId = StationIdMap[this.stationId][count - 1];
                enumerator.MoveNext();
                var child = ParseToken(ref enumerator, targetStationId, isReverse, isTotalControl);
                if (enumerator.Current.Length != count && enumerator.Current.All(c => c == ']'))
                {
                    throw new InvalidOperationException("]が閉じられていません");
                }

                enumerator.MoveNext();
                result.AddRange(child);
            }
            else if (token == "(")
            {
                enumerator.MoveNext();
                var target = ParseToken(ref enumerator, stationId, true, isTotalControl);
                if (target.Count != 1)
                {
                    throw new InvalidOperationException("反位の対象がないか、複数あります");
                }

                if (enumerator.Current is not ")")
                {
                    throw new InvalidOperationException(")が閉じられていません");
                }

                enumerator.MoveNext();
                item = target[0];
                result.Add(item);
            }
            else if (token == "但")
            {
                var left = result;
                enumerator.MoveNext();
                var right = ParseToken(ref enumerator, stationId, isReverse, isTotalControl);
                List<LockItem> child = new();
                if (left.Count == 1)
                {
                    child.Add(left[0]);
                }
                else
                {
                    child.Add(new()
                    {
                        Name = NameOr,
                        StationId = stationId,
                        IsReverse = isReverse ? NR.Reversed : NR.Normal,
                        Children = left
                    });
                }

                if (right.Count == 1)
                {
                    child.Add(right[0]);
                }
                else
                {
                    child.Add(new()
                    {
                        Name = NameOr,
                        StationId = stationId,
                        IsReverse = isReverse ? NR.Reversed : NR.Normal,
                        Children = right
                    });
                }

                result =
                [
                    new()
                    {
                        Name = NameAnd,
                        StationId = stationId,
                        IsReverse = isReverse ? NR.Reversed : NR.Normal,
                        Children = child
                    }
                ];
            }
            else if (token == "又は")
            {
                // 又は条件は、何も書かなかった場合と同義なので、処理を持たせずスキップでOK
                enumerator.MoveNext();
            }
            else
            {
                item = new()
                {
                    Name = token,
                    StationId = stationId,
                    isTotalControl = isTotalControl,
                    IsReverse = isReverse ? NR.Reversed : NR.Normal
                };
                result.Add(item);
                enumerator.MoveNext();
            }
        }

        return result;
    }

    public class LockItem
    {
        public string Name { get; set; }
        public string StationId { get; set; }
        public bool isTotalControl { get; set; }
        public int RouteLockGroup { get; set; }
        public NR IsReverse { get; set; }
        public List<LockItem> Children { get; set; } = [];
    }

    private string CalcSwitchingMachineName(string start, string stationId)
    {
        return $"{stationId}_W{start}";
    }

    private string CalcLeverName(string start, string stationId)
    {
        if (stationId == "")
        {
            stationId = this.stationId;
        }

        return $"{stationId}_{start.Replace("R", "").Replace("L", "")}";
    }

    private string CalcButtonName(string end, string stationId)
    {
        if (stationId == "")
        {
            stationId = this.stationId;
        }

        return $"{stationId}_{end.Replace("(", "").Replace(")", "")}P";
    }

    private string ConvertHalfWidthToFullWidth(string halfWidth)
    {
        return halfWidth.Replace('ｲ', 'イ').Replace('ﾛ', 'ロ');
    }

    private string CalcRouteName(string start, string end, string stationId)
    {
        if (stationId == "")
        {
            stationId = this.stationId;
        }

        return $"{stationId}_{start}{(end.StartsWith('(') ? "" : end)}";
    }
}