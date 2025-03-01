using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.HostedService;

public class InitDbHostedService(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    private TickService? _tickService;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await InitDb(context, cancellationToken);
        await InitRendoTable(context, cancellationToken);

        _tickService = new(serviceScopeFactory);
    }

    private async Task InitDb(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var jsonstring = await File.ReadAllTextAsync("./Data/DBBase.json", cancellationToken);
        var DBBase = JsonSerializer.Deserialize<DBBasejson>(jsonstring);
        if (DBBase != null)
        {
            var initializer = new DbInitializer(DBBase, context, cancellationToken);
            await initializer.Initialize();
        }
    }

    private async Task InitRendoTable(ApplicationDbContext context, CancellationToken cancellationToken)
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
            var initializer = new DbRendoTableInitializer(stationId, records, context, cancellationToken);
            initializers.Add(initializer);
            await initializer.InitializeObjects();
        }

        foreach (var initializer in initializers)
        {
            await initializer.InitializeLocks();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_tickService == null)
        {
            return;
        }
        await _tickService.Stop();
    }
}

file class DbInitializer(DBBasejson DBBase, ApplicationDbContext context, CancellationToken cancellationToken)
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
        // 既に登録済みの信号情報を取得
        var signalNames = (await context.Signals
            .Select(s => s.Name)
            .ToListAsync(cancellationToken)).ToHashSet();
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
                trackCircuitId = await context.TrackCircuits
                    .Where(tc => tc.Name == trackCircuitName)
                    .Select(tc => tc.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            context.Signals.Add(new()
            {
                Name = signalData.Name,
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
        foreach (var signalTypeData in DBBase.signalTypeList)
        {
            if (context.SignalTypes.Any(st => st.Name == signalTypeData.Name))
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

internal partial class DbRendoTableInitializer
{
    const string NameSwitchingMachine = "転てつ器";

    private static readonly Dictionary<string, List<string>> stationIdMap = new()
    {
        // 津崎: 浜園
        { "TH71", ["TH70"] },
        // 浜園: 津崎
        { "TH70", ["TH71"]}
    };

    [GeneratedRegex(@"\d+")]
    private static partial Regex RegexIntParse();

    // 鎖状欄からてこ名を取得するための正規表現、[]が増えたときの対応のため、nameの番号は逆順にしてある
    [GeneratedRegex(@"(?:\[{2}(?<name3>[^]]+?)\]{2})|(?:\[(?<name2>[^]]+?)\])|(?<name1r>\(\S+?\))|(?<name1>[^\s\(]+)")]
    private static partial Regex RegexLockColumn();

    // 信号制御欄から統括制御とそれ以外の部位に分けるための正規表現
    [GeneratedRegex(@"^(.*?)(?:(\(\([^\)\s]+\)\))\s*)*$")]
    private static partial Regex RegexSignalControl();

    // ReSharper disable InconsistentNaming
    private readonly string stationId;
    private readonly List<RendoTableCSV> rendoTableCsvs;
    private readonly ApplicationDbContext context;
    private readonly CancellationToken cancellationToken;
    private readonly List<string> otherStations;
    // ReSharper restore InconsistentNaming

    internal DbRendoTableInitializer(
        string stationId,
        List<RendoTableCSV> rendoTableCsvs,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        this.stationId = stationId;
        this.rendoTableCsvs = rendoTableCsvs;
        this.context = context;
        this.cancellationToken = cancellationToken;
        otherStations = stationIdMap.GetValueOrDefault(stationId) ?? [];
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
                        SwitchEndTime = DateTime.UtcNow.AddDays(-1)
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
                    OperatedAt = DateTime.UtcNow.AddDays(-1)
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
            int? approachLockTime = matches.Success ? int.Parse(matches.Value): null;

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
        
        // 進路のDict
        var routes = interlockingObjects
            .OfType<Route>()
            .ToDictionary(r => r.Name, r => r);
        // 転てつ器のDict
        var switchingMachines = interlockingObjects
            .OfType<SwitchingMachine>()
            .ToDictionary(sm => sm.Name, sm => sm);
        // その他のオブジェクトのDict
        var otherObjects = interlockingObjects
            .Where(io => io is not SwitchingMachine)
            .ToDictionary(io => io.Name, io => io);
        
        var searchSwitchingMachine = new Func<LockItem, Task<InterlockingObject?>>(
            item => Task.FromResult<InterlockingObject?>(
                switchingMachines.GetValueOrDefault(CalcSwitchingMachineName(item.Name, item.StationId))));
        var searchOtherObjects = new Func<LockItem, Task<InterlockingObject?>>(item =>
            {
                // 進路 or 軌道回路の場合はこちら
                var key = ConvertHalfWidthToFullWidth(CalcRouteName(item.Name, "", item.StationId));
                var result = otherObjects.GetValueOrDefault(key);
                if (result != null)
                {
                    return Task.FromResult<InterlockingObject?>(result);
                }
                // Todo: てこの場合、名前を整数部分のみにする
                key = CalcLeverName(item.Name, item.StationId);
                return Task.FromResult(otherObjects.GetValueOrDefault(key));
            });

        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            var routeName = CalcRouteName(rendoTableCsv.Start, rendoTableCsv.End, stationId);
            var route = routes.GetValueOrDefault(routeName);
            if (route == null)
            {
                // Todo: Warningを出す
                continue;
            }
            // Todo: CTC進路の場合と、その他の進路の場合で処理を分ける
            // 鎖錠欄(転てつ器)
            await RegisterLocks(rendoTableCsv.LockToSwitchingMachine, route.Id, searchSwitchingMachine, LockType.Lock);
            // 鎖錠欄(そのほか)
            await RegisterLocks(rendoTableCsv.LockToRoute, route.Id, searchOtherObjects, LockType.Lock);
            
            // 信号制御欄
            var matchSignalControl = RegexSignalControl().Match(rendoTableCsv.SignalControl);
            await RegisterLocks(matchSignalControl.Groups[1].Value, route.Id, searchOtherObjects, LockType.SignalControl);
            // 統括制御
            foreach(Capture capture in matchSignalControl.Groups[2].Captures)
            {
                var rootRoute = routes.GetValueOrDefault(CalcRouteName(capture.Value, "", stationId));
                if (rootRoute == null)
                {
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
            var switchingMachine = await searchSwitchingMachine(new()
            {
                Name = rendoTableCsv.Start,
                StationId = stationId
            });
            if (switchingMachine == null)
            {
                // Todo: 例外を出す
                continue;
            }

            await RegisterLocks(rendoTableCsv.SignalControl, switchingMachine.Id, searchOtherObjects, LockType.Detector);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task RegisterLocks(string lockString, ulong objectId, Func<LockItem, Task<InterlockingObject?>> searchTargetObjects,
        LockType lockType)
    {
        var lockItems = CalcLockItems(lockString);
        foreach (var lockItem in lockItems)
        {
            var targetObject = await searchTargetObjects(lockItem);
            if (targetObject == null)
            {
                // Todo: 例外を出す
                continue;
            }

            Lock lockObject = new()
            {
                ObjectId = objectId,
                Type = lockType
            };
            context.Locks.Add(lockObject);
            context.LockConditionObjects.Add(new()
            {
                Lock = lockObject,
                ObjectId = targetObject.Id,
                IsReverse = lockItem.IsReverse,
                Type = LockConditionType.Object
            });
        }
    }

    private List<LockItem> CalcLockItems(string lockString)
    {
        if (string.IsNullOrWhiteSpace(lockString))
        {
            return [];
        }

        var matches = RegexLockColumn().Matches(lockString);

        return matches
            .SelectMany(match =>
            {
                for (var i = 0; i < 3; i++)
                {
                    // グループを取得 
                    var group = match.Groups[$"name{i + 1}"];
                    // 0番目の場合、反位パターンがあるのでそれも取得
                    if (!group.Success && i == 0)
                    {
                        group = match.Groups[$"name{i + 1}r"];
                    }

                    // グループが取得できなかった場合、次のループへ
                    if (!group.Success)
                    {
                        continue;
                    }

                    // 実際の行 (21) や 21 など
                    var column = group.Value;
                    var isReverse = column.StartsWith('(');
                    // Objectの名前
                    // 例 (21) -> 21
                    // Todo: 片鎖状に対応する
                    var targetName = isReverse ? column.Substring(1, column.Length - 2) : column;
                    // 対象オブジェクトの駅ID
                    var objectStationId = stationId;
                    if (i > 0)
                    {
                        objectStationId = otherStations[i - 1];
                    }
                    return
                    [
                        new()
                        {
                            Name = targetName,
                            StationId = objectStationId,
                            IsReverse = isReverse ? NR.Reversed : NR.Normal
                        }
                    ];
                }

                // Todo: 例外吐かせた方が良い
                return new List<LockItem>();
            })
            .ToList();
    }

    private class LockItem
    {
        public string Name { get; set; }
        public string StationId { get; set; }
        public int RouteLockGroup { get; set; }
        public NR IsReverse { get; set; }
    }

    private string CalcSwitchingMachineName(string start, string stationId)
    {
        return $"{stationId}_w{start}";
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