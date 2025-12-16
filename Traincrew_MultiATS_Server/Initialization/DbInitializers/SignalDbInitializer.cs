using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes signal entities and related data in the database
/// </summary>
public class SignalDbInitializer(ApplicationDbContext context, ILogger<SignalDbInitializer> logger)
    : BaseDbInitializer(context, logger)
{
    /// <summary>
    ///     Initialize signals from CSV data
    /// </summary>
    public async Task InitializeSignalsAsync(List<SignalCsv> signalDataList,
        CancellationToken cancellationToken = default)
    {
        // 軌道回路情報を取得
        var trackCircuits = await _context.TrackCircuits
            .Select(tc => new { tc.Id, tc.Name })
            .ToDictionaryAsync(tc => tc.Name, tc => tc.Id, cancellationToken);

        // 既に登録済みの信号情報を取得
        var signalNames = (await _context.Signals
            .Select(s => s.Name)
            .ToListAsync(cancellationToken)).ToHashSet();

        // 駅マスタから停車場を取得
        var stations = await _context.Stations
            .Where(station => station.IsStation)
            .ToListAsync(cancellationToken);

        // DirectionRoutesを事前に取得してDictionaryに格納
        var directionRoutes = await _context.DirectionRoutes
            .ToDictionaryAsync(dr => dr.Name, dr => dr.Id, cancellationToken);

        var addedCount = 0;
        // 信号情報登録
        foreach (var signalData in signalDataList)
        {
            // 既に登録済みの場合、スキップ
            if (signalNames.Contains(signalData.Name))
            {
                continue;
            }

            // 軌道回路初期化
            ulong trackCircuitId = 0;
            // 明示的に指定された軌道回路名がある場合はそれを使用
            if (signalData.TrackCircuitName != null)
            {
                trackCircuits.TryGetValue(signalData.TrackCircuitName, out trackCircuitId);
            }
            // それ以外で閉塞信号機の場合、閉塞信号機の軌道回路を使う
            else if (signalData.Name.StartsWith("上り閉塞") || signalData.Name.StartsWith("下り閉塞"))
            {
                var trackCircuitName = $"{signalData.Name.Replace("閉塞", "")}T";
                trackCircuits.TryGetValue(trackCircuitName, out trackCircuitId);
            }

            var stationId = stations
                .Where(s => signalData.Name.StartsWith(s.Name))
                .Select(s => s.Id)
                .FirstOrDefault();

            // 方向進路および方向の初期化
            ulong? directionRouteLeftId = null;
            ulong? directionRouteRightId = null;
            if (signalData.DirectionRouteLeft != null)
            {
                if (!directionRoutes.TryGetValue(signalData.DirectionRouteLeft, out var directionRouteId))
                {
                    throw new InvalidOperationException($"方向進路が見つかりません: {signalData.DirectionRouteLeft}");
                }

                directionRouteLeftId = directionRouteId;
            }

            if (signalData.DirectionRouteRight != null)
            {
                if (!directionRoutes.TryGetValue(signalData.DirectionRouteRight, out var directionRouteId))
                {
                    throw new InvalidOperationException($"方向進路が見つかりません: {signalData.DirectionRouteRight}");
                }

                directionRouteRightId = directionRouteId;
            }

            LR? direction = signalData.Direction != null
                ? signalData.Direction == "L" ? LR.Left : signalData.Direction == "R" ? LR.Right : null
                : null;

            _context.Signals.Add(new()
            {
                Name = signalData.Name,
                StationId = stationId,
                TrackCircuitId = trackCircuitId > 0 ? trackCircuitId : null,
                TypeName = signalData.TypeName,
                SignalState = new()
                {
                    IsLighted = true
                },
                DirectionRouteLeftId = directionRouteLeftId,
                DirectionRouteRightId = directionRouteRightId,
                Direction = direction
            });
            addedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized {Count} signals", addedCount);
    }

    /// <summary>
    ///     Initialize next signal relationships from CSV data
    /// </summary>
    public async Task InitializeNextSignalsAsync(List<SignalCsv> signalDataList,
        CancellationToken cancellationToken = default)
    {
        const int maxDepth = 4;
        foreach (var signalData in signalDataList)
        {
            var nextSignalNames = signalData.NextSignalNames ?? [];
            foreach (var nextSignalName in nextSignalNames)
            {
                // Todo: ここでN+1問題が発生しているので、改善したほうが良いかも
                // 既に登録済みの場合、スキップ
                if (_context.NextSignals.Any(ns =>
                        ns.SignalName == signalData.Name && ns.TargetSignalName == nextSignalName))
                {
                    continue;
                }

                _context.NextSignals.Add(new()
                {
                    SignalName = signalData.Name,
                    SourceSignalName = signalData.Name,
                    TargetSignalName = nextSignalName,
                    Depth = 1
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var allSignals = await _context.Signals.ToListAsync(cancellationToken);
        var nextSignalList = await _context.NextSignals
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
            var nextNextSignalDict = await _context.NextSignals
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
                        if (_context.NextSignals.Any(ns =>
                                ns.SignalName == signal.Name && ns.TargetSignalName == nextNextSignal))
                        {
                            continue;
                        }

                        _context.NextSignals.Add(new()
                        {
                            SignalName = signal.Name,
                            SourceSignalName = nextSignal,
                            TargetSignalName = nextNextSignal,
                            Depth = depth
                        });
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
            }
        }

        _logger.LogInformation("Initialized next signals up to depth {MaxDepth}", maxDepth);
    }

    /// <summary>
    ///     Initialize signal route relationships from CSV data
    /// </summary>
    public async Task InitializeSignalRoutesAsync(List<SignalCsv> signalDataList,
        CancellationToken cancellationToken = default)
    {
        var signalRoutes = await _context.SignalRoutes
            .Include(sr => sr.Route)
            .ToListAsync(cancellationToken);
        var routes = await _context.Routes
            .ToDictionaryAsync(r => r.Name, cancellationToken);

        foreach (var signal in signalDataList)
        {
            foreach (var routeName in signal.RouteNames ?? [])
            {
                // Todo: FW 全探索なので改善したほうがいいかも
                if (signalRoutes.Any(sr => sr.SignalName == signal.Name && sr.Route.Name == routeName))
                {
                    continue;
                }

                if (!routes.TryGetValue(routeName, out var route))
                    // Todo: 例外を出す
                {
                    continue;
                }

                _context.SignalRoutes.Add(new()
                {
                    SignalName = signal.Name,
                    RouteId = route.Id
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initialized signal routes");
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // This method is not used for SignalDbInitializer as it requires CSV data
        // Use InitializeSignalsAsync, InitializeNextSignalsAsync, and InitializeSignalRoutesAsync instead
        await Task.CompletedTask;
    }
}