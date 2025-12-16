using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Initialization;

public partial class DbRendoTableInitializer
{
    private const string NameSwitchingMachine = "転てつ器";
    private const string NameClosure = "閉そく";
    private const string PrefixTrackCircuitDown = "下り";
    private const string PrefixTrackCircuitUp = "上り";
    private const string NameAnd = "and";
    private const string NameOr = "or";
    private const string NameNot = "not";

    private static readonly Dictionary<string, List<string>> StationIdMap = new()
    {
        // 赤山町: 西赤山、三郷
        { "TH58", ["TH59", "TH57"] },
        // 西赤山: 赤山町
        { "TH59", ["TH58"] },
        // 日野森: 高見沢
        { "TH61", ["TH62"] },
        // 高見沢: 水越、日野森
        { "TH62", ["TH63", "TH61"] },
        // 水越: 藤江、高見沢
        { "TH63", ["TH64", "TH62"] },
        // 藤江: 大道寺、水越
        { "TH64", ["TH65", "TH63"] },
        // 大道寺: 江ノ原信号場、藤江
        { "TH65", ["TH66S", "TH64"] },
        // 江ノ原信号場: 大道寺
        { "TH66S", ["TH65"] },
        // 新野崎:
        { "TH67", [] },
        // 浜園: 津崎
        { "TH70", ["TH71"] },
        // 津崎: 浜園
        { "TH71", ["TH70"] },
        // 駒野: 館浜、閉そく
        { "TH75", ["TH76", NameClosure] },
        // 館浜: 駒野
        { "TH76", ["TH75"] }
    };

    public DbRendoTableInitializer(
        string stationId,
        List<RendoTableCSV> rendoTableCsvs,
        ApplicationDbContext context,
        IDateTimeRepository dateTimeRepository,
        ILogger<DbRendoTableInitializer> logger,
        CancellationToken cancellationToken)
    {
        this.stationId = stationId;
        this.rendoTableCsvs = rendoTableCsvs;
        this.context = context;
        this.dateTimeRepository = dateTimeRepository;
        this.logger = logger;
        this.cancellationToken = cancellationToken;
        otherStations = (StationIdMap.GetValueOrDefault(stationId) ?? [])
            .Where(s => s != NameClosure)
            .ToList();
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex RegexIntParse();

    // てこ名を抽出するための正規表現
    [GeneratedRegex(@"(\d+)(R|L)(Z?)")]
    private static partial Regex RegexLeverParse();

    // てこ名を抽出するための正規表現(Full Version)
    [GeneratedRegex(@"^(\d+)(R|L)(Z?)$")]
    private static partial Regex RegexLeverParseFullMatch();

    // 軌道回路名を抽出するための正規表現
    [GeneratedRegex(@"[A-Z\dｲﾛ]+T")]
    private static partial Regex RegexTrackCircuitParse();

    // 閉塞軌道回路名を抽出するための正規表現
    [GeneratedRegex(@"^(\d+)T$")]
    private static partial Regex RegexClosureTrackCircuitParse();

    // 信号制御欄から統括制御とそれ以外の部位に分けるための正規表現
    [GeneratedRegex(@"^(.*?)(?:\(\(([^\)\s]+)\)\)\s*)*$")]
    private static partial Regex RegexSignalControl();

    // 連動図表の鎖錠欄の諸々のトークンを抽出するための正規表現
    [GeneratedRegex(@"\[\[|\]\]|\(\(|\)\)|\[|\]|\{|\}|\(|\)|｢|｣|但\s+\d+秒|但|又は|[A-Z\dｲﾛ]+")]
    private static partial Regex TokenRegex();

    internal async Task InitializeObjects()
    {
        PreprocessCsv();
        await InitLever();
        await InitDirectionSelfControlLever();
        await InitRouteCentralControlLever();
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
        var previousApproachLock = "";
        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            if (string.IsNullOrWhiteSpace(rendoTableCsv.Name) || rendoTableCsv.Name.StartsWith('同'))
                rendoTableCsv.Name = oldName;
            else
                oldName = rendoTableCsv.Name;

            if (string.IsNullOrWhiteSpace(rendoTableCsv.Start)) rendoTableCsv.Start = previousStart;

            // てこ番が違う場合
            if (previousStart != rendoTableCsv.Start)
            {
                preivousLockTime = rendoTableCsv.ApproachTime;
                previousApproachLock = rendoTableCsv.ApproachLock;
            }
            else
            {
                rendoTableCsv.ApproachTime = preivousLockTime;
                rendoTableCsv.ApproachLock = previousApproachLock;
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
                leverType = LeverType.Route;
            else if (rendoTableCsv.Name.StartsWith(NameSwitchingMachine))
                leverType = LeverType.SwitchingMachine;
            else if (rendoTableCsv.Name.Contains("方向"))
                leverType = LeverType.Direction;
            else
                continue;

            // てこがすでに存在する場合はcontinue
            var name = CalcLeverName(rendoTableCsv.Start, stationId);
            if (rendoTableCsv.Start.Length <= 0 || !leverNames.Add(name)) continue;

            // 転てつ器の場合、転てつ器を登録
            SwitchingMachine? switchingMachine = null;
            if (leverType == LeverType.SwitchingMachine)
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

            Lever lever = new()
            {
                Name = name,
                Type = ObjectType.Lever,
                LeverType = leverType,
                SwitchingMachine = switchingMachine,
                LeverState = new()
                {
                    IsReversed = leverType == LeverType.Direction ? LCR.Left : LCR.Center
                }
            };
            context.Levers.Add(lever);

            // 方向てこの場合、方向進路を登録
            if (leverType == LeverType.Direction)
            {
                DirectionRoute directionRoute = new()
                {
                    Lever = lever,
                    Type = ObjectType.DirectionRoute,
                    Name = CalcDirectionLeverName(rendoTableCsv.Start, stationId),
                    DirectionRouteState = new()
                    {
                        isLr = LR.Left,
                        IsFlRelayRaised = RaiseDrop.Drop,
                        IsLfysRelayRaised = RaiseDrop.Drop,
                        IsRfysRelayRaised = RaiseDrop.Drop,
                        IsLyRelayRaised = RaiseDrop.Drop,
                        IsRyRelayRaised = RaiseDrop.Drop,
                        IsLRelayRaised = RaiseDrop.Drop,
                        IsRRelayRaised = RaiseDrop.Drop
                    }
                };
                context.DirectionRoutes.Add(directionRoute);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitDirectionSelfControlLever()
    {
        // 該当駅の全てのてこを取得
        var leverNames = (await context.DirectionSelfControlLevers
            .Where(l => l.Name.StartsWith(stationId))
            .Select(l => l.Name)
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            if (!rendoTableCsv.Name.Contains("開放")) continue;

            // てこがすでに存在する場合はcontinue
            var name = CalcLeverName(rendoTableCsv.Start, stationId);
            if (rendoTableCsv.Start.Length <= 0 || !leverNames.Add(name)) continue;

            context.DirectionSelfControlLevers.Add(new()
            {
                Name = name,
                Type = ObjectType.DirectionSelfControlLever,
                DirectionSelfControlLeverState = new()
                {
                    IsInsertedKey = false,
                    IsReversed = NR.Normal
                }
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitRouteCentralControlLever()
    {
        // 該当駅の全てのてこを取得
        var leverNames = (await context.RouteCentralControlLevers
            .Where(l => l.Name.StartsWith(stationId))
            .Select(l => l.Name)
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            if (!rendoTableCsv.Name.Contains("CTC")) continue;

            // てこがすでに存在する場合はcontinue
            var name = CalcLeverName(rendoTableCsv.Start, stationId);
            if (rendoTableCsv.Start.Length <= 0 || !leverNames.Add(name)) continue;

            context.RouteCentralControlLevers.Add(new()
            {
                Name = name,
                Type = ObjectType.RouteCentralControlLever,
                RouteCentralControlLeverState = new()
                {
                    IsInsertedKey = false,
                    IsReversed = NR.Normal
                }
            });
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
            if (string.IsNullOrWhiteSpace(rendoTableCsv.End) || rendoTableCsv.End is "L" or "R") continue;

            // ボタン名を生成
            var buttonName = CalcButtonName(rendoTableCsv.End, stationId);
            if (existingButtonNames.Contains(buttonName)) continue;

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

        List<(Route, ulong, LR, string?)> routes = [];
        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            // RouteTypeを決定
            RouteType routeType;
            if (rendoTableCsv.Name.Contains("場内"))
                routeType = RouteType.Arriving;
            else if (rendoTableCsv.Name.Contains("出発"))
                routeType = RouteType.Departure;
            else if (rendoTableCsv.Name.Contains("誘導"))
                routeType = RouteType.Guide;
            else if (rendoTableCsv.Name.Contains("入換信号"))
                routeType = RouteType.SwitchSignal;
            else if (rendoTableCsv.Name.Contains("入換標識"))
                routeType = RouteType.SwitchRoute;
            else
                continue;

            // 進路名を生成
            var routeName = CalcRouteName(rendoTableCsv.Start, rendoTableCsv.End, stationId);

            if (existingRouteNames.Contains(routeName)) continue;

            existingRouteNames.Add(routeName);

            // 接近鎖状時素決定
            var matches = RegexIntParse().Match(rendoTableCsv.ApproachTime);
            int? approachLockTime = matches.Success ? int.Parse(matches.Value) : null;

            // 進路を追加
            // Todo: 将来的にTcNameを駅名+進路名にする
            Route route = new()
            {
                Name = routeName,
                TcName = routeName,
                RouteType = routeType,
                RootId = null,
                Indicator = rendoTableCsv.Indicator,
                ApproachLockTime = approachLockTime,
                RouteState = new()
                {
                    IsLeverRelayRaised = RaiseDrop.Drop,
                    IsRouteRelayRaised = RaiseDrop.Drop,
                    IsSignalControlRaised = RaiseDrop.Drop,
                    IsApproachLockMRRaised = RaiseDrop.Drop,
                    IsApproachLockMSRaised = RaiseDrop.Drop,
                    IsRouteLockRaised = RaiseDrop.Drop
                }
            };
            var match = RegexLeverParse().Match(rendoTableCsv.Start);
            var leverName = CalcLeverName(match.Groups[1].Value + match.Groups[3].Value, stationId);
            var direction = match.Groups[2].Value == "L" ? LR.Left : LR.Right;
            var buttonName = CalcButtonName(rendoTableCsv.End, stationId);
            if (!leverDictionary.TryGetValue(leverName, out var lever)) continue;

            // 着点のない進路は着点をnullとして登録
            if (!string.IsNullOrWhiteSpace(rendoTableCsv.End))
                routes.Add((route, lever.Id, direction, buttonName));
            else
                routes.Add((route, lever.Id, direction, null));

            context.Routes.Add(route);
        }

        await context.SaveChangesAsync(cancellationToken);

        // 進路とてこと着点ボタンの関連付けを追加
        foreach (var (route, leverId, direction, buttonName) in routes)
        {
            context.RouteLeverDestinationButtons.Add(new()
            {
                RouteId = route.Id,
                LeverId = leverId,
                Direction = direction,
                DestinationButtonName = buttonName
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitLocks()
    {
        // 必要なオブジェクトを取得
        var interlockingObjects = await context.InterlockingObjects
            .Where(io => io.Name.StartsWith(stationId) || otherStations.Any(s => io.Name.StartsWith(s)))
            .ToListAsync(cancellationToken);
        var locks = (await context.Locks
                .Select(l => l.ObjectId)
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var locksByRouteCentralControlLevers = (await context.LockConditionByRouteCentralControlLevers
                .Select(l => new
                {
                    l.RouteCentralControlLeverId, l.RouteId
                })
                .ToListAsync(cancellationToken)
            )
            .ToHashSet();
        var throwOutControl = await context.ThrowOutControls
            .ToListAsync(cancellationToken);
        var leverNamesById = await context.Levers
            .ToDictionaryAsync(l => l.Id, l => l.Name, cancellationToken);
        var routeLeverDestinationButtons = await context.RouteLeverDestinationButtons
            .ToListAsync(cancellationToken);

        // 進路のDict
        var routes = interlockingObjects
            .OfType<Route>()
            .ToList();
        var routesByName = routes
            .ToDictionary(r => r.Name, r => r);
        var routesById = routes
            .ToDictionary(r => r.Id, r => r);
        // 方向進路のDict
        var directionRoutes = interlockingObjects
            .OfType<DirectionRoute>()
            .ToList();
        var directionRoutesByName = directionRoutes
            .ToDictionary(r => r.Name, r => r);
        // CTC切替てこのDict
        var routeCentralControlLevers = interlockingObjects
            .OfType<RouteCentralControlLever>()
            .ToList();
        var routeCentralControlLeversByName = routeCentralControlLevers
            .ToDictionary(l => l.Name, l => l);
        // 転てつ器のDict
        var switchingMachines = interlockingObjects
            .OfType<SwitchingMachine>()
            .ToDictionary(sm => sm.Name, sm => sm);
        // その他のオブジェクトのDict
        var otherObjects = interlockingObjects
            .Where(io => io is not SwitchingMachine)
            .ToDictionary(io => io.Name, io => io);
        // てこ->進路へのDict
        var routeIdsByLeverName = routeLeverDestinationButtons
            .GroupBy(x => x.LeverId)
            .ToDictionary(
                g => leverNamesById[g.Key],
                g => g.Select(x => x.RouteId).ToList()
            );
        var routeIdsByButtonName = routeLeverDestinationButtons
            .Where(x => x.DestinationButtonName != null)
            .GroupBy(x => x.DestinationButtonName!)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.RouteId).ToList()
            );
        // 統括制御のDict
        var throwOutControlBySourceId = throwOutControl
            .GroupBy(toc => toc.SourceId)
            .ToDictionary(
                g => g.Key,
                g => g.ToList()
            );
        var searchSwitchingMachine = new Func<LockItem, Task<List<InterlockingObject>>>(item =>
        {
            // Todo: もっときれいに書けるはず
            var targetObject =
                switchingMachines.GetValueOrDefault(CalcSwitchingMachineName(item.Name, item.StationId));
            List<InterlockingObject> result;
            if (targetObject != null)
                result = [targetObject];
            else
                result = [];

            return Task.FromResult(result);
        });
        var searchDirectionRoutes = new Func<LockItem, Task<InterlockingObject?>>(async item =>
        {
            // 方向進路
            if (!(item.Name.EndsWith('L') || item.Name.EndsWith('R'))) return null;

            var key = CalcDirectionLeverName(item.Name[..^1], item.StationId);
            return otherObjects.GetValueOrDefault(key);
        });
        var searchOtherObjects = new Func<LockItem, Task<List<InterlockingObject>>>(async item =>
        {
            // 進路(単一) or 軌道回路の場合はこちら
            var key = ConvertHalfWidthToFullWidth(CalcRouteName(item.Name, "", item.StationId));
            var value = otherObjects.GetValueOrDefault(key);
            if (value != null) return [value];

            // 方向進路の場合はこちら
            var directionRoute = await searchDirectionRoutes(item);
            if (directionRoute != null) return [directionRoute];


            // てこ名指定の場合はこちら=>そのてこを始点としたすべての進路を取得
            var leverFullMatch = RegexLeverParseFullMatch().Match(item.Name);
            if (leverFullMatch.Success)
            {
                var leverName = CalcLeverName(
                    leverFullMatch.Groups[1].Value + leverFullMatch.Groups[3].Value, item.StationId);
                var routeIds = routeIdsByLeverName.GetValueOrDefault(leverName);
                if (routeIds != null) return routeIds.Select(InterlockingObject (r) => routesById[r]).ToList();
            }

            // 統括進路はこちら
            var leverMatch = RegexLeverParse().Match(item.Name);
            if (leverMatch.Success)
            {
                // てこ
                var leverName = CalcLeverName(
                    leverMatch.Groups[1].Value + leverMatch.Groups[3].Value, item.StationId);
                // 着点ボタン
                var buttonName = CalcButtonName(
                    item.Name[(leverMatch.Index + leverMatch.Length)..],
                    item.StationId);

                // 統括制御から、該当する進路を導き出す
                // てこに該当する進路すべて
                var startRouteIds = routeIdsByLeverName.GetValueOrDefault(leverName, []);
                // 該当する統括制御を選ぶ(てこに該当する進路=>統括制御=>着点てこに該当する進路)
                var targetThrowOutControls = startRouteIds
                    .SelectMany(r => throwOutControlBySourceId.GetValueOrDefault(r, []))
                    .Where(toc => routeIdsByButtonName[buttonName].Contains(toc.TargetId))
                    .ToList();
                var targetThrowOutControl = targetThrowOutControls.FirstOrDefault();
                if (targetThrowOutControls.Count >= 2)
                    throw new InvalidOperationException($"統括制御が2つ以上見つかりました: {item.Name}");

                if (targetThrowOutControl != null)
                {
                    var startRoute = routesById[targetThrowOutControl.SourceId];
                    var endRoute = routesById[targetThrowOutControl.TargetId];
                    return [startRoute, endRoute];
                }
            }

            return [];
        });
        var searchClosureTrackCircuit = new Func<LockItem, Task<List<InterlockingObject>>>(async item =>
        {
            var match = RegexClosureTrackCircuitParse().Match(item.Name);
            string trackCircuitName;
            // 閉塞軌道回路
            if (match.Success)
            {
                var trackCircuitNumber = int.Parse(match.Groups[1].Value);
                var prefix = trackCircuitNumber % 2 == 0 ? PrefixTrackCircuitUp : PrefixTrackCircuitDown;
                trackCircuitName = $"{prefix}{trackCircuitNumber}T";
            }
            // 単線の諸々軌道回路
            else
            {
                trackCircuitName = item.Name;
            }

            var trackCircuit = await context.TrackCircuits
                .FirstOrDefaultAsync(tc => tc.Name == trackCircuitName, cancellationToken);
            if (trackCircuit == null) return [];

            return [trackCircuit];
        });
        var searchObjectsForApproachLock = new Func<LockItem, Task<List<InterlockingObject>>>(async item =>
        {
            if (item.StationId == NameClosure) return await searchClosureTrackCircuit(item);

            var result = await searchOtherObjects(item);
            if (result.Count > 0) return result;

            // 接近鎖錠の場合、閉塞軌道回路も探す
            return await searchClosureTrackCircuit(item);
        });

        int? approachLockTime = null;
        // 通常進路用の処理
        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            // Todo: 通常進路と方向てこ/開放てこで処理を分ける
            var routeName = CalcRouteName(rendoTableCsv.Start, rendoTableCsv.End, stationId);
            var route = routesByName.GetValueOrDefault(routeName);
            if (route == null)
                // Todo: 通常進路であればエラーを出す
                continue;

            // 既に何らか登録済みの場合、Continue
            if (locks.Contains(route.Id)) continue;

            // 鎖錠欄(転てつ器)
            await RegisterLocks(
                rendoTableCsv.LockToSwitchingMachine, route.Id, searchSwitchingMachine, LockType.Lock, true);
            // 鎖錠欄(そのほか)
            await RegisterLocks(rendoTableCsv.LockToRoute, route.Id, searchOtherObjects, LockType.Lock);

            // 信号制御欄
            var matchSignalControl = RegexSignalControl().Match(rendoTableCsv.SignalControl);
            await RegisterLocks(matchSignalControl.Groups[1].Value, route.Id, searchOtherObjects,
                LockType.SignalControl);
            // 統括制御は、サーバーマスタから読み込む
            // 進路鎖錠
            await RegisterLocks(rendoTableCsv.RouteLock, route.Id, searchOtherObjects,
                LockType.Route, isRouteLock: true);

            // 接近鎖錠
            await RegisterLocks(rendoTableCsv.ApproachLock, route.Id, searchObjectsForApproachLock,
                LockType.Approach);
            await RegisterFinalTrackCircuitId(
                rendoTableCsv.ApproachLock, route, searchObjectsForApproachLock);
        }

        // 方向進路用の処理
        // 通常進路用の処理
        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            var routeName = CalcDirectionLeverName(rendoTableCsv.Start, stationId);
            var route = directionRoutesByName.GetValueOrDefault(routeName);
            if (route == null)
                // Todo: 方向進路ならエラーを出す
                continue;

            // 鎖錠欄(軌道回路)
            if (!locks.Contains(route.Id))
                await RegisterLocks(rendoTableCsv.LockToRoute, route.Id, searchObjectsForApproachLock, LockType.Lock);

            // 鎖錠欄(鎖錠 または 被片鎖錠)
            var isLr = rendoTableCsv.End == "L" ? LR.Left : LR.Right;
            await RegisterDirectionRouteLock(rendoTableCsv.LockToSwitchingMachine, route,
                searchDirectionRoutes, isLr);
        }

        // CTC切換てこの処理
        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            var leverName = CalcLeverName(rendoTableCsv.Start, stationId);
            var routeCentralControlLever = routeCentralControlLeversByName.GetValueOrDefault(leverName);
            if (routeCentralControlLever == null || rendoTableCsv.End != "R")
                // Todo: CTC切換てこでrouteCentralControlLeverがnullならエラーを返す
                continue;

            var lockItems = CalcLockItems(rendoTableCsv.LockToSwitchingMachine, false)
                .SelectMany(lockItem => lockItem.Children)
                .ToList();
            var targetRoutes = (await Task.WhenAll(lockItems.Select(searchOtherObjects)))
                .SelectMany(x => x.OfType<Route>())
                .ToList();
            var entities = targetRoutes
                .Select(r => new LockConditionByRouteCentralControlLever
                {
                    RouteCentralControlLeverId = routeCentralControlLever.Id,
                    RouteId = r.Id
                })
                .Where(condition => !locksByRouteCentralControlLevers.Contains(new
                {
                    condition.RouteCentralControlLeverId, condition.RouteId
                }));

            context.LockConditionByRouteCentralControlLevers.AddRange(entities);
            await context.SaveChangesAsync(cancellationToken);
        }

        // 転てつ器のてっ査鎖錠を処理する
        foreach (var rendoTableCsv in rendoTableCsvs)
        {
            // Todo: 江ノ原61~78転てつ器は、Traincrewが実装していないので、一旦スルーする
            if (stationId == "TH66S" && rendoTableCsv.Start == "61") break;

            var targetSwitchingMachines = await searchSwitchingMachine(new()
            {
                Name = rendoTableCsv.Start,
                StationId = stationId
            });
            if (targetSwitchingMachines.Count == 0)
                // Todo: 例外を出す
                continue;

            var switchingMachine = targetSwitchingMachines[0];

            // 既に何らか登録済みの場合、Continue
            if (locks.Contains(switchingMachine.Id)) continue;

            await RegisterLocks(rendoTableCsv.SignalControl, switchingMachine.Id, searchOtherObjects,
                LockType.Detector);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task RegisterLocks(string lockString, ulong objectId,
        Func<LockItem, Task<List<InterlockingObject>>> searchTargetObjects,
        LockType lockType,
        bool registerSwitchingMachineRoute = false,
        bool isRouteLock = false)
    {
        var lockItems = CalcLockItems(lockString, isRouteLock);

        // ループは基本的に進路鎖状用で、それ以外の場合はループは１回のみ
        for (var i = 0; i < lockItems.Count; i++)
        {
            var lockItem = lockItems[i];
            Lock lockObject = new()
            {
                ObjectId = objectId,
                Type = lockType,
                RouteLockGroup = i + 1
            };
            context.Locks.Add(lockObject);

            await RegisterLocksInner(
                lockItem, lockObject, null,
                registerSwitchingMachineRoute ? objectId : null, searchTargetObjects);
        }
    }

    private async Task RegisterLocksInner(
        LockItem item,
        Lock lockObject,
        LockCondition? parent,
        ulong? routeIdForSwitchingMachineRoute,
        Func<LockItem, Task<List<InterlockingObject>>> searchTargetObjects)
    {
        LockCondition? current = null;
        if (item.Name == NameOr)
            current = new()
            {
                Lock = lockObject,
                Type = LockConditionType.Or,
                Parent = parent
            };

        if (item.Name == NameAnd)
            current = new()
            {
                Lock = lockObject,
                Type = LockConditionType.And,
                Parent = parent
            };

        if (item.Name == NameNot)
            current = new()
            {
                Lock = lockObject,
                Type = LockConditionType.Not,
                Parent = parent
            };

        // or か and か not の場合、
        if (current != null)
        {
            if (item.Children.Count == 0) return;

            context.LockConditions.Add(current);
            foreach (var child in item.Children)
            {
                await RegisterLocksInner(
                    child, lockObject, current, routeIdForSwitchingMachineRoute, searchTargetObjects);
            }

            return;
        }

        // 単一オブジェクトに対する処理
        var targetObjects = await searchTargetObjects(item);
        if (targetObjects.Count == 0)
            // Todo: 方向てこ、開放てこに対する処理
            throw new InvalidOperationException($"対象のオブジェクトが見つかりません: {item.StationId} {item.Name}");

        if (targetObjects.Count == 1)
        {
            // 方向てこだった場合、方向も指定する
            LR? isLr = null;
            if (targetObjects[0] is DirectionRoute) isLr = item.Name.EndsWith('L') ? LR.Left : LR.Right;

            context.LockConditionObjects.Add(new()
            {
                Lock = lockObject,
                ObjectId = targetObjects[0].Id,
                Parent = parent,
                TimerSeconds = item.TimerSeconds,
                IsReverse = item.IsReverse,
                IsLR = isLr,
                Type = LockConditionType.Object
            });

            return;
        }

        current = new()
        {
            Lock = lockObject,
            Type = LockConditionType.And,
            Parent = parent
        };
        context.LockConditions.Add(current);
        foreach (var targetObject in targetObjects)
        {
            // 方向てこだった場合、方向も指定する
            LR? isLr = null;
            if (targetObjects[0] is DirectionRoute) isLr = item.Name.EndsWith('L') ? LR.Left : LR.Right;

            context.LockConditionObjects.Add(new()
            {
                Lock = lockObject,
                Parent = current,
                ObjectId = targetObject.Id,
                TimerSeconds = item.TimerSeconds,
                IsReverse = item.IsReverse,
                IsLR = isLr,
                Type = LockConditionType.Object
            });
            if (routeIdForSwitchingMachineRoute == null || targetObjects[0] is not SwitchingMachine switchingMachine)
            {
            }
        }
    }

    private async Task RegisterFinalTrackCircuitId(string lockString, Route route,
        Func<LockItem, Task<List<InterlockingObject>>> searchTargetObjects)
    {
        var lockItems = CalcLockItems(lockString, false);
        if (lockItems.Count == 0) return;

        // 接近鎖錠のパース時はlockItemsが1つの想定
        var lockItem = lockItems[0];
        await RegisterFinalTrackCircuitIdInner(lockItem, route, searchTargetObjects);
    }

    private async Task<bool> RegisterFinalTrackCircuitIdInner(LockItem lockItem, Route route,
        Func<LockItem, Task<List<InterlockingObject>>> searchTargetObjects)
    {
        List<InterlockingObject> targetObjects;
        if (lockItem.Name is NameOr or NameAnd or NameNot)
        {
            // or, and, not の場合は、子どもを右から見ていって軌道回路っぽいやつを探す
            var reversedChildren = lockItem.Children.ToList();
            reversedChildren.Reverse();
            foreach (var child in reversedChildren)
                // 登録ができたらreturnする
            {
                if (await RegisterFinalTrackCircuitIdInner(child, route, searchTargetObjects))
                    return true;
            }
        }

        targetObjects = await searchTargetObjects(lockItem);
        var reversedTargetObjects = targetObjects.ToList();
        reversedTargetObjects.Reverse();
        foreach (var targetObject in reversedTargetObjects)
        {
            if (targetObject is not TrackCircuit trackCircuit) continue;

            // 接近鎖錠欄の最終軌道回路を登録
            route.ApproachLockFinalTrackCircuitId = trackCircuit.Id;
            context.Update(route);
            return true;
        }

        // 進路の最終閉塞軌道回路が見つからなかった
        return false;
    }

    private async Task RegisterDirectionRouteLock(string lockString, DirectionRoute route,
        Func<LockItem, Task<InterlockingObject?>> searchTargetObjects,
        LR isLr)
    {
        var lockItems = CalcLockItems(lockString, false);
        if (lockItems.Count == 0)
        {
            logger.Log(LogLevel.Warning,
                "被片鎖錠てこも、鎖錠てこも見つかりません。処理をスキップします。{}", route.Name);
            return;
        }

        if (lockItems.Count == 2)
        {
            logger.Log(LogLevel.Warning,
                "被片鎖錠てこ または 鎖錠てこが複数見つかりました。処理をスキップします。{}", route.Name);
            return;
        }

        var lockItem = lockItems[0];
        var item = await searchTargetObjects(lockItem);
        if (item == null)
        {
            if (lockItem.StationId == "TH57")
            {
                logger.Log(LogLevel.Warning,
                    "三郷駅です。処理をスキップします。{}", lockItem.Name);
                return;
            }

            throw new InvalidOperationException($"対象の方向進路が見つかりません: {lockItem.StationId} {lockItem.Name}");
        }

        var direction = lockItem.Name.EndsWith('L') ? LR.Left : LR.Right;

        // Lてこに対する
        if (isLr == LR.Left)
        {
            // 被片鎖錠
            if (lockItem.isLocked)
            {
                route.LSingleLockedLeverId = item.Id;
                route.LSingleLockedLeverDirection = direction;
            }
            // 鎖錠
            else
            {
                route.LLockLeverId = item.Id;
                route.LLockLeverDirection = direction;
            }
        }
        // Rてこに対する
        else
        {
            // 被片鎖錠
            if (lockItem.isLocked)
            {
                route.RSingleLockedLeverId = item.Id;
                route.RSingleLockedLeverDirection = direction;
            }
            // 鎖錠
            else
            {
                route.RLockLeverId = item.Id;
                route.RLockLeverDirection = direction;
            }
        }

        context.Update(route);
    }

    public List<LockItem> CalcLockItems(string lockString, bool isRouteLock)
    {
        var tokens = TokenRegex().Matches(lockString)
            .Select(m => m.Value)
            .ToList();
        var enumerator = tokens.GetEnumerator();
        enumerator.MoveNext();
        var lockItems = ParseToken(ref enumerator, stationId, isRouteLock, false, false, false);
        return isRouteLock ? lockItems : [GroupByAndIfMultipleCondition(lockItems)];
    }

    private List<LockItem> ParseToken(ref List<string>.Enumerator enumerator,
        string stationId,
        bool isRouteLock,
        bool isReverse,
        bool isTotalControl,
        bool isLocked)
    {
        List<LockItem> result = [];
        // なぜかCanBeNullなはずなのにそれを無視してしまうので
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        while (enumerator.Current != null)
        {
            var token = enumerator.Current;
            // 括弧とじならbreakし、再起元に判断を委ねる
            if (token is ")" or "]" or "]]" or "}" or "｣") break;

            LockItem item;
            if (token == "{")
            {
                // 意味カッコ
                enumerator.MoveNext();
                var child = ParseToken(ref enumerator, stationId, isRouteLock, isReverse, isTotalControl, isLocked);
                if (enumerator.Current != "}") throw new InvalidOperationException("}が閉じられていません");

                enumerator.MoveNext();
                result.AddRange(child);
            }
            else if (token == "((")
            {
                // 統括制御(連動図表からではなく、別CSVから取り込みなのでスキップ)
                enumerator.MoveNext();
                ParseToken(ref enumerator, stationId, isRouteLock, isReverse, true, isLocked);
                if (enumerator.Current != "))") throw new InvalidOperationException("))が閉じられていません");

                enumerator.MoveNext();
            }
            else if (token.StartsWith('['))
            {
                // 別駅所属のObject
                var count = token.Length;
                var targetStationId = StationIdMap[this.stationId][count - 1];
                enumerator.MoveNext();
                var child = ParseToken(ref enumerator, targetStationId, isRouteLock, isReverse, isTotalControl,
                    isLocked);
                if (enumerator.Current.Length != count || enumerator.Current.Any(c => c != ']'))
                    throw new InvalidOperationException("]が閉じられていません");

                enumerator.MoveNext();
                result.AddRange(child);
            }
            else if (token == "(")
            {
                // 進路鎖錠パース時は進路鎖錠のグループ、それ以外の場合は反位を渡す
                enumerator.MoveNext();
                var target = ParseToken(
                    ref enumerator,
                    stationId,
                    isRouteLock,
                    !isRouteLock, // 進路鎖状なら定位を渡す、それ以外なら反位を渡す
                    isTotalControl,
                    isLocked);
                if (!isRouteLock && target.Count != 1) throw new InvalidOperationException("反位の対象がないか、複数あります");

                if (enumerator.Current is not ")") throw new InvalidOperationException(")が閉じられていません");

                enumerator.MoveNext();
                if (isRouteLock)
                    item = new()
                    {
                        Name = NameAnd,
                        StationId = stationId,
                        IsReverse = isReverse ? NR.Reversed : NR.Normal,
                        Children = target
                    };
                else
                    item = target[0];

                result.Add(item);
            }
            else if (token == "｢")
            {
                // 被鎖錠
                enumerator.MoveNext();
                var child = ParseToken(ref enumerator, stationId, isRouteLock, isReverse, isTotalControl, true);
                if (enumerator.Current != "｣") throw new InvalidOperationException("｣が閉じられていません");

                enumerator.MoveNext();
                result.AddRange(child);
            }
            else if (token.StartsWith('但') && token.EndsWith('秒'))
            {
                // 時素条件
                result[^1].TimerSeconds = int.Parse(RegexIntParse()
                    .Match(token)
                    .Value);
                enumerator.MoveNext();
            }
            else if (token == "但")
            {
                // 但条件(左辺 or not右辺)
                var left = result;
                enumerator.MoveNext();
                var right = ParseToken(ref enumerator, stationId, isRouteLock, isReverse, isTotalControl, isLocked);
                List<LockItem> child =
                [
                    // 左辺
                    GroupByAndIfMultipleCondition(left),
                    // 右辺
                    new()
                    {
                        Name = NameNot,
                        StationId = stationId,
                        IsReverse = isReverse ? NR.Reversed : NR.Normal,
                        Children = [GroupByAndIfMultipleCondition(right)]
                    }
                ];
                result =
                [
                    new()
                    {
                        Name = NameOr,
                        StationId = stationId,
                        IsReverse = isReverse ? NR.Reversed : NR.Normal,
                        Children = child
                    }
                ];
            }
            else if (token == "又は")
            {
                // Todo: or条件
                var left = result;
                enumerator.MoveNext();
                var right = ParseToken(ref enumerator, stationId, isRouteLock, isReverse, isTotalControl, isLocked);
                List<LockItem> child =
                [
                    GroupByAndIfMultipleCondition(left)
                ];
                // 右辺がorならそのまま子供を追加
                if (right is [{ Name: NameOr }])
                    child.AddRange(right[0].Children);
                // それ以外ならグルーピングして追加
                else
                    child.Add(GroupByAndIfMultipleCondition(right));

                result =
                [
                    new()
                    {
                        Name = NameOr,
                        StationId = stationId,
                        IsReverse = isReverse ? NR.Reversed : NR.Normal,
                        Children = child
                    }
                ];
            }
            else
            {
                item = new()
                {
                    Name = token,
                    StationId = stationId,
                    isTotalControl = isTotalControl,
                    isLocked = isLocked,
                    IsReverse = isReverse ? NR.Reversed : NR.Normal
                };
                result.Add(item);
                enumerator.MoveNext();
            }
        }

        return result;
    }

    private LockItem GroupByAndIfMultipleCondition(List<LockItem> lockItems)
    {
        if (lockItems.Count == 1) return lockItems[0];

        return new()
        {
            Children = lockItems,
            Name = NameAnd
        };
    }

    private string CalcSwitchingMachineName(string start, string stationId)
    {
        return $"{stationId}_W{start}";
    }

    private string CalcLeverName(string start, string stationId)
    {
        if (stationId == "") stationId = this.stationId;

        return $"{stationId}_{start.Replace("R", "").Replace("L", "")}";
    }

    private string CalcDirectionLeverName(string start, string stationId)
    {
        if (stationId == "") stationId = this.stationId;

        return $"{stationId}_{start}F";
    }

    private string CalcButtonName(string end, string stationId)
    {
        if (stationId == "") stationId = this.stationId;

        return $"{stationId}_{end.Replace("(", "").Replace(")", "")}P";
    }

    private string ConvertHalfWidthToFullWidth(string halfWidth)
    {
        return halfWidth.Replace('ｲ', 'イ').Replace('ﾛ', 'ロ');
    }

    private string CalcRouteName(string start, string end, string stationId)
    {
        if (stationId == "") stationId = this.stationId;

        return $"{stationId}_{start}{(end.StartsWith('(') ? "" : end)}";
    }

    public class LockItem
    {
        public string Name { get; set; }
        public string StationId { get; set; }
        public bool isTotalControl { get; set; }
        public bool isLocked { get; set; }
        public int? TimerSeconds { get; set; }
        public NR IsReverse { get; set; }
        public List<LockItem> Children { get; set; } = [];
    }

    // ReSharper disable InconsistentNaming
    private readonly string stationId;
    private readonly List<RendoTableCSV> rendoTableCsvs;
    private readonly ApplicationDbContext context;
    private readonly IDateTimeRepository dateTimeRepository;
    private readonly CancellationToken cancellationToken;
    private readonly List<string> otherStations;

    private readonly ILogger<DbRendoTableInitializer> logger;
    // ReSharper restore InconsistentNaming
}