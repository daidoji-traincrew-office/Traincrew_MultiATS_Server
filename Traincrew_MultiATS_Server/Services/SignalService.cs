using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.NextSignal;
using Traincrew_MultiATS_Server.Repositories.Signal;
using Traincrew_MultiATS_Server.Repositories.SignalRoute;
using Route = Traincrew_MultiATS_Server.Models.Route;

namespace Traincrew_MultiATS_Server.Services;

public class SignalService(
    ISignalRepository signalRepository,
    ISignalRouteRepository signalRouteRepository,
    INextSignalRepository nextSignalRepository)
{
    /// <summary>
    /// 指定した軌道回路から見える信号機の現示データを計算する
    /// </summary>
    /// <param name="trackCircuits">対象となる軌道回路のリスト</param>
    /// <param name="isUp">上り方向かどうか</param>
    /// <returns>信号機の現示データのリスト</returns>
    public async Task<List<SignalData>> GetSignalIndicationDataByTrackCircuits(List<TrackCircuit> trackCircuits, bool isUp)
    {
        
        // 該当軌道回路の信号機を全取得
        var closeSignalName = await GetSignalNamesByTrackCircuits(trackCircuits, isUp);
        // 各信号機の１つ先の信号機も取得
        var nextSignalName = await nextSignalRepository.GetByNamesAndDepth(closeSignalName, 1);
        // 取得した信号機を結合
        var signalName = closeSignalName
            .Concat(nextSignalName.Select(x => x.TargetSignalName))
            .Distinct()
            .ToList();
        // 現示計算
        var signalIndications = await CalcSignalIndication(signalName);
        return signalIndications
            .Select(pair => ToSignalData(
                pair.Key,
                pair.Value)
            )
            .ToList();
    }

    /// <summary>
    /// 全信号機の現示を計算する
    /// </summary>
    /// <param name="shouldSendOnly">実装済み区間のみ送るか、すべて送るかどうか</param>
    /// <returns>信号機の現示データのリスト</returns>
    public async Task<List<SignalData>> CalcAllSignalIndication(bool shouldSendOnly = true)
    {
        // 1. 全信号の詳細情報を取得
        var allSignals = await signalRepository.GetSignalsForCalcIndication(shouldSendOnly);
        var signals = allSignals.ToDictionary(x => x.Name);

        // 2. depth=1の先の信号情報を全取得
        var nextSignalsDepth1 = await nextSignalRepository.GetAllByDepth(1);

        // NextSignal情報を辞書化
        var nextSignalDict = nextSignalsDepth1
            .GroupBy(x => x.SourceSignalName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.TargetSignalName).ToList()
            );

        // 3. SignalRouteを全取得
        var routes = await signalRouteRepository.GetAllRoutes();

        // 4. 信号名をHashSetに入れて、結果用Dictionaryを定義
        var remainingSignals = allSignals.Select(x => x.Name).ToHashSet();
        var result = new Dictionary<string, SignalIndication>();

        // 5-7. HashSetから信号名を取得し、現示を計算。HashSetが空になるまで繰り返し
        while (remainingSignals.Count > 0)
        {
            // HashSetから信号名を一つ取得
            var signalName = remainingSignals.First();

            // 信号現示を計算（計算過程で先の信号機の現示を計算した場合、HashSetから削除される）
            CalcSignalIndicationRecursive(signalName, signals, nextSignalDict, routes, result, remainingSignals);
        }

        return result
            .Select(pair => ToSignalData(pair.Key, ToPhase(pair.Value)))
            .ToList();
    }

    /// <summary>
    /// 指定した信号機名のリストから各信号機の現示を計算する
    /// </summary>
    /// <param name="signalNames">現示を計算する信号機名のリスト</param>
    /// <param name="getDetailedIndication">具体的な現示まで計算するか(進行・減速・注意・警戒)まで計算するか。
    ///                                     進行を示す現示であるか？のみに興味がある場合falseを指定すると軽くなる</param>
    /// <returns>信号機名をキーとし、信号現示を値とする辞書</returns>
    public async Task<Dictionary<string, Phase>> CalcSignalIndication(List<string> signalNames, bool getDetailedIndication = true)
    {
        // まず、先の信号機名を取得
        var nextSignals = getDetailedIndication
            ? await nextSignalRepository.GetNextSignalByNamesOrderByDepthDesc(signalNames) 
            : [];
        // その上で、必要な信号と情報をすべて取得する
        var signalList = await signalRepository.GetSignalsByNamesForCalcIndication(
            signalNames.Concat(nextSignals.Select(x => x.TargetSignalName)).ToList());
        var routes = await signalRouteRepository.GetRoutesBySignalNames(
            signalList.Select(x => x.Name));
        // 計算するべき全信号のリスト
        var signals = signalList.ToDictionary(x => x.Name);
        var nextSignalDict = nextSignals
            .GroupBy(x => x.SourceSignalName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.TargetSignalName).ToList()
            );
        var cache = new Dictionary<string, SignalIndication>();
        signalNames.ForEach(name => CalcSignalIndication(name, signals, nextSignalDict, routes, cache));
        // Todo: 無灯火時、無灯火と返すようにする
        // 信号機の現示を計算して返す
        return cache.ToDictionary(
            kv => kv.Key,
            kv => ToPhase(kv.Value)
        );
    }

    /// <summary>
    /// 指定した軌道回路から見える信号機名を取得する
    /// </summary>
    /// <param name="trackCircuits">対象となる軌道回路のリスト</param>
    /// <param name="isUp">上り方向かどうか</param>
    /// <returns>信号機名のリスト</returns>
    private async Task<List<string>> GetSignalNamesByTrackCircuits(List<TrackCircuit> trackCircuits, bool isUp)
    {
        return await signalRepository.GetSignalNamesByTrackCircuits(
            trackCircuits.Select(tc => tc.Name).ToList(), isUp);
    }

    /// <summary>
    /// 単一の信号機の現示を計算する（HashSetを使った全体計算用）
    /// </summary>
    /// <param name="signalName">計算対象の信号機名</param>
    /// <param name="signals">信号機の辞書</param>
    /// <param name="nextSignalDict">次の信号機の辞書</param>
    /// <param name="routeDict">ルートの辞書</param>
    /// <param name="cache">計算結果のキャッシュ</param>
    /// <param name="remainingSignals">計算待ちの信号機名のHashSet</param>
    /// <returns>計算された信号現示</returns>
    private static SignalIndication CalcSignalIndicationRecursive(
        string signalName,
        Dictionary<string, Signal> signals,
        Dictionary<string, List<string>> nextSignalDict,
        Dictionary<string, List<Route>> routeDict,
        Dictionary<string, SignalIndication> cache,
        HashSet<string> remainingSignals
    )
    {
        // すでに計算済みの信号機の場合、その結果を返す
        if (cache.TryGetValue(signalName, out var signalIndication))
        {
            return signalIndication;
        }

        var result = SignalIndication.R;
        var routes = routeDict.GetValueOrDefault(signalName, []);
        if (
            // 信号機が存在している
            signals.TryGetValue(signalName, out var signal)
            // 絶対信号機の場合、信号制御リレーが扛上している
            && (routes.Count == 0 || routes.Any(route => route.RouteState.IsSignalControlRaised == RaiseDrop.Raise))
            // 許容信号機の場合、対象軌道回路が短絡していない
            && !(signal.TrackCircuit?.TrackCircuitState.IsShortCircuit ?? false)
            // 単線区間の場合、方向が正しい
            && (signal.DirectionRouteLeftId == null ||
                signal.DirectionRouteLeft.DirectionRouteState.isLr == signal.Direction)
            && (signal.DirectionRouteRightId == null ||
                signal.DirectionRouteRight.DirectionRouteState.isLr == signal.Direction)
        )
        {
            // 次の信号機名を取得
            var nextSignalNames = nextSignalDict.GetValueOrDefault(signalName, []);
            // 次の信号機の情報を取得
            var nextSignalIndications = nextSignalNames
                .Select(x => CalcSignalIndicationRecursive(x, signals, nextSignalDict, routeDict, cache, remainingSignals))
                .ToList();
            // 次の信号機の中で最も高い現示を取得
            var nextSignalIndication = SignalIndication.R;
            if (nextSignalIndications.Count != 0)
            {
                nextSignalIndication = nextSignalIndications.Max();
            }

            // 信号機の種類によって、信号の現示を計算する
            result = GetIndication(signal.Type, nextSignalIndication);
        }

        // 計算結果をキャッシュに追加し、HashSetから削除
        cache[signalName] = result;
        remainingSignals.Remove(signalName);
        return result;
    }

    /// <summary>
    /// 単一の信号機の現示を計算する（具体的な現示計算時は、再帰的に次の信号機の現示も考慮）
    /// </summary>
    /// <param name="signalName">計算対象の信号機名</param>
    /// <param name="signals">信号機の辞書</param>
    /// <param name="nextSignalDict">次の信号機の辞書</param>
    /// <param name="routeDict">ルートの辞書</param>
    /// <param name="cache">計算結果のキャッシュ</param>
    /// <returns>計算された信号現示</returns>
    private static SignalIndication CalcSignalIndication(
        string signalName,
        Dictionary<string, Signal> signals,
        Dictionary<string, List<string>> nextSignalDict,
        Dictionary<string, List<Route>> routeDict,
        Dictionary<string, SignalIndication> cache
    )
    {
        // すでに計算済みの信号機の場合、その結果を返す
        if (cache.TryGetValue(signalName, out var signalIndication))
        {
            return signalIndication;
        }

        var result = SignalIndication.R;
        var routes = routeDict.GetValueOrDefault(signalName, []);
        if (
            // 信号機が存在している
            signals.TryGetValue(signalName, out var signal)
            // 絶対信号機の場合、信号制御リレーが扛上している
            && (routes.Count == 0 || routes.Any(route => route.RouteState.IsSignalControlRaised == RaiseDrop.Raise))
            // 許容信号機の場合、対象軌道回路が短絡していない
            && !(signal.TrackCircuit?.TrackCircuitState.IsShortCircuit ?? false)
            // 単線区間の場合、方向が正しい
            && (signal.DirectionRouteLeftId == null ||
                signal.DirectionRouteLeft.DirectionRouteState.isLr == signal.Direction)
            && (signal.DirectionRouteRightId == null ||
                signal.DirectionRouteRight.DirectionRouteState.isLr == signal.Direction)
        )
        {
            // 次の信号機名を取得
            var nextSignalNames = nextSignalDict.GetValueOrDefault(signalName, []);
            // 次の信号機の情報を取得
            var nextSignalIndications = nextSignalNames
                .Select(x => CalcSignalIndication(x, signals, nextSignalDict, routeDict, cache))
                .ToList();
            // 次の信号機の中で最も高い現示を取得
            var nextSignalIndication = SignalIndication.R;
            if (nextSignalIndications.Count != 0)
            {
                nextSignalIndication = nextSignalIndications.Max();
            }

            // 信号機の種類によって、信号の現示を計算する
            result = GetIndication(signal.Type, nextSignalIndication);
        }

        cache[signalName] = result;
        return result;
    }

    /// <summary>
    /// 指定した駅IDに関連する信号機名を取得する
    /// </summary>
    /// <param name="stationIds">対象となる駅IDのリスト</param>
    /// <returns>信号機名のリスト</returns>
    public async Task<List<string>> GetSignalNamesByStationIds(List<string> stationIds)
    {
        return await signalRepository.GetSignalNamesByStationIds(stationIds);
    }

    /// <summary>
    /// 信号機の種類と次の信号機の現示から、現在の信号機の現示を決定する
    /// </summary>
    /// <param name="signalType">信号機の種類</param>
    /// <param name="nextSignalIndication">次の信号機の現示</param>
    /// <returns>決定された信号現示</returns>
    private static SignalIndication GetIndication(SignalType signalType, SignalIndication nextSignalIndication)
    {
        return nextSignalIndication switch
        {
            SignalIndication.G => signalType.GIndication,
            SignalIndication.YG => signalType.YGIndication,
            SignalIndication.Y => signalType.YIndication,
            SignalIndication.YY => signalType.YYIndication,
            _ => signalType.RIndication,
        };
    }

    /// <summary>
    /// SignalIndicationをPhaseに変換する
    /// </summary>
    /// <param name="indication">変換元のSignalIndication</param>
    /// <returns>変換されたPhase</returns>
    private static Phase ToPhase(SignalIndication indication)
    {
        return indication switch
        {
            SignalIndication.G => Phase.G,
            SignalIndication.YG => Phase.YG,
            SignalIndication.Y => Phase.Y,
            SignalIndication.YY => Phase.YY,
            _ => Phase.R,
        };
    }

    /// <summary>
    /// 信号機名と現示からSignalDataオブジェクトを作成する
    /// </summary>
    /// <param name="signalName">信号機名</param>
    /// <param name="phase">信号機の現示</param>
    /// <returns>作成されたSignalDataオブジェクト</returns>
    public static SignalData ToSignalData(string signalName, Phase phase)
    {
        return new()
        {
            Name = signalName,
            phase = phase
        };
    }
}