using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.DirectionRoute;
using Traincrew_MultiATS_Server.Repositories.General;
using Traincrew_MultiATS_Server.Repositories.NextSignal;
using Traincrew_MultiATS_Server.Repositories.Route;
using Traincrew_MultiATS_Server.Repositories.Signal;
using Traincrew_MultiATS_Server.Repositories.SignalRoute;
using Traincrew_MultiATS_Server.Repositories.Station;
using Traincrew_MultiATS_Server.Repositories.TrackCircuit;

namespace Traincrew_MultiATS_Server.Initialization.DbInitializers;

/// <summary>
///     Initializes signal entities and related data in the database
/// </summary>
public class SignalDbInitializer(
    ILogger<SignalDbInitializer> logger,
    ISignalRepository signalRepository,
    INextSignalRepository nextSignalRepository,
    ISignalRouteRepository signalRouteRepository,
    ITrackCircuitRepository trackCircuitRepository,
    IStationRepository stationRepository,
    IDirectionRouteRepository directionRouteRepository,
    IRouteRepository routeRepository,
    IGeneralRepository generalRepository)
    : BaseDbInitializer
{
    /// <summary>
    ///     Initialize signals from CSV data
    /// </summary>
    public async Task InitializeSignalsAsync(List<SignalCsv> signalDataList,
        CancellationToken cancellationToken = default)
    {
        // 軌道回路情報を取得
        var trackCircuits = await trackCircuitRepository.GetAllIdsForName(cancellationToken);

        // 既に登録済みの信号情報を取得
        var signalNames = await signalRepository.GetAllNames(cancellationToken);

        // 駅マスタから停車場を取得
        var stations = await stationRepository.GetWhereIsStation();

        // DirectionRoutesを事前に取得してDictionaryに格納
        var directionRoutes = await directionRouteRepository.GetIdsByNameAsync(cancellationToken);

        var signalList = new List<Signal>();
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

            signalList.Add(new()
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
        }

        await generalRepository.AddAll(signalList);
        logger.LogInformation("Initialized {Count} signals", signalList.Count);
    }

    /// <summary>
    ///     Initialize next signal relationships from CSV data
    /// </summary>
    public async Task InitializeNextSignalsAsync(List<SignalCsv> signalDataList,
        CancellationToken cancellationToken = default)
    {
        const int maxDepth = 4;

        // 既存のNextSignalsを事前に取得してHashSetに格納（N+1問題の回避）
        var existingNextSignals = await nextSignalRepository.GetAllAsync(cancellationToken);
        var existingNextSignalSet = existingNextSignals
            .Select(ns => (ns.SignalName, ns.TargetSignalName))
            .ToHashSet();

        var nextSignalList = new List<NextSignal>();
        foreach (var signalData in signalDataList)
        {
            var nextSignalNames = signalData.NextSignalNames ?? [];
            foreach (var nextSignalName in nextSignalNames)
            {
                // 既に登録済みの場合、スキップ
                if (existingNextSignalSet.Contains((signalData.Name, nextSignalName)))
                {
                    continue;
                }

                var nextSignal = new NextSignal
                {
                    SignalName = signalData.Name,
                    SourceSignalName = signalData.Name,
                    TargetSignalName = nextSignalName,
                    Depth = 1
                };

                nextSignalList.Add(nextSignal);
                existingNextSignals.Add(nextSignal);
                // 追加したものもHashSetに追加して、後続の処理で重複を防ぐ
                existingNextSignalSet.Add((signalData.Name, nextSignalName));
            }
        }

        await generalRepository.AddAll(nextSignalList);

        // 中継信号機のリンクも追加する
        // 例:
        // depth=1のリンクとして以下があった場合
        // - 上り閉塞6 -> 上り閉塞8
        // - 上り閉塞8中継 -> 上り閉塞8
        // 以下のリンクも追加する
        // - 上り閉塞6 -> 上り閉塞8中継

        var depth1NextSignals = await nextSignalRepository.GetAllByDepth(1);

        // 中継信号機のリンクを特定 (例: "X中継 -> Y")
        var relaySignalLinks = depth1NextSignals
            .Where(ns => ns.SignalName.Contains("中継"))
            .ToList();

        // 中継信号機へのリンクを宣言的に生成
        var relayLinksToAdd = relaySignalLinks
            .SelectMany(relayLink =>
            {
                var relaySignalName = relayLink.SignalName;  // X中継
                var targetSignalName = relayLink.TargetSignalName;  // Y

                // "Z -> Y" のようなリンクを探して、"Z -> X中継" のペアを作成
                return depth1NextSignals
                    .Where(ns => ns.TargetSignalName == targetSignalName && ns.SignalName != relaySignalName)
                    .Select(signalToTarget => new
                    {
                        SourceSignalName = signalToTarget.SignalName,  // Z
                        RelaySignalName = relaySignalName  // X中継
                    });
            })
            // 既に "Z -> X中継" のリンクが存在する場合は除外
            .Where(link => !depth1NextSignals.Any(ns =>
                ns.SignalName == link.SourceSignalName && ns.TargetSignalName == link.RelaySignalName))
            .Distinct()
            // NextSignalオブジェクトを生成
            .Select(link => new NextSignal
            {
                SignalName = link.SourceSignalName,
                SourceSignalName = link.SourceSignalName,
                TargetSignalName = link.RelaySignalName,
                Depth = 1
            })
            .ToList();

        await generalRepository.AddAll(relayLinksToAdd);

        var allSignals = await signalRepository.GetAll();
        var nextSignalsBySignalName = existingNextSignals
            .Where(ns => ns.Depth == 1)
            .GroupBy(ns => ns.SignalName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(ns => ns.TargetSignalName).ToList()
            );

        // Todo: このロジック、絶対テスト書いたほうがいい(若干複雑な処理をしてしまったので)
        for (var depth = 2; depth <= maxDepth; depth++)
        {
            var nextNextSignalDict = existingNextSignals
                .Where(ns => ns.Depth == depth - 1)
                .GroupBy(ns => ns.SignalName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(ns => ns.TargetSignalName).ToList()
                );
            var depthNextSignalList = new List<NextSignal>();
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
                    if (!nextSignalsBySignalName.TryGetValue(nextSignal, out var nnSignals))
                    {
                        continue;
                    }

                    foreach (var nnSignalName in nnSignals)
                    {
                        // 既に登録済みの場合、スキップ
                        if (existingNextSignalSet.Contains((signal.Name, nnSignalName)))
                        {
                            continue;
                        }

                        var nnSignal = new NextSignal
                        {
                            SignalName = signal.Name,
                            SourceSignalName = nextSignal,
                            TargetSignalName = nnSignalName,
                            Depth = depth
                        };

                        depthNextSignalList.Add(nnSignal);
                        existingNextSignals.Add(nnSignal);
                        // 追加したものもHashSetに追加して、後続の処理で重複を防ぐ
                        existingNextSignalSet.Add((signal.Name, nnSignal.TargetSignalName));
                    }
                }
            }

            // 各depthの処理が終わった後にまとめて保存
            await generalRepository.AddAll(depthNextSignalList);
        }

        logger.LogInformation("Initialized next signals up to depth {MaxDepth}", maxDepth);
    }

    /// <summary>
    ///     Initialize signal route relationships from CSV data
    /// </summary>
    public async Task InitializeSignalRoutesAsync(List<SignalCsv> signalDataList,
        CancellationToken cancellationToken = default)
    {
        var signalRoutes = await signalRouteRepository.GetAllWithRoutesAsync(cancellationToken);
        var routes = await routeRepository.GetIdsByName(cancellationToken);

        var signalRouteList = new List<SignalRoute>();
        foreach (var signal in signalDataList)
        {
            foreach (var routeName in signal.RouteNames ?? [])
            {
                // Todo: FW 全探索なので改善したほうがいいかも
                if (signalRoutes.Any(sr => sr.SignalName == signal.Name && sr.Route.Name == routeName))
                {
                    continue;
                }

                if (!routes.TryGetValue(routeName, out var routeId))
                    // Todo: 例外を出す
                {
                    continue;
                }

                signalRouteList.Add(new()
                {
                    SignalName = signal.Name,
                    RouteId = routeId
                });
            }
        }

        await generalRepository.AddAll(signalRouteList);
        logger.LogInformation("Initialized signal routes");
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // This method is not used for SignalDbInitializer as it requires CSV data
        // Use InitializeSignalsAsync, InitializeNextSignalsAsync, and InitializeSignalRoutesAsync instead
        await Task.CompletedTask;
    }
}