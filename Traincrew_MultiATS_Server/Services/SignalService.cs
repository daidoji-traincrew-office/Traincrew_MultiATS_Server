using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.NextSignal;
using Traincrew_MultiATS_Server.Repositories.Signal;

namespace Traincrew_MultiATS_Server.Services;

public class SignalService(
    ISignalRepository signalRepository,
    INextSignalRepository nextSignalRepository)
{
    public async Task<Dictionary<string, Phase>> CalcSignalIndication(List<string> signalNames)
    {
        // まず、先の信号機名を取得
        var nextSignals = await nextSignalRepository.GetNextSignalByNamesOrderByDepthDesc(signalNames);
        // その上で、必要な信号と情報をすべて取得する
        var signalList = await signalRepository.GetSignalsByNamesForCalcIndication(
            signalNames.Concat(nextSignals.Select(x => x.TargetSignalName)).ToList());
        // 計算するべき全信号のリスト
        var signals = signalList.ToDictionary(x => x.Name);
        var nextSignalDict = nextSignals
            .GroupBy(x => x.SourceSignalName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.TargetSignalName).ToList()
            );
        var cache = new Dictionary<string, SignalIndication>();
        signalNames.ForEach(name => CalcSignalIndication(name, signals, nextSignalDict, cache));
        // Todo: 無灯火時、無灯火と返すようにする
        // 信号機の現示を計算して返す
        return cache.ToDictionary(
            kv => kv.Key,
            kv => ToPhase(kv.Value)
        );
    }

    public async Task<List<string>> GetSignalNamesByTrackCircuits(List<TrackCircuit> trackCircuits, bool isUp)
    {
        return await signalRepository.GetSignalNamesByTrackCircuits(
            trackCircuits.Select(tc => tc.Name).ToList(), isUp);
    }

    private static SignalIndication CalcSignalIndication(
        string signalName,
        Dictionary<string, Signal> signals,
        Dictionary<string, List<string>> nextSignalDict,
        Dictionary<string, SignalIndication> cache
    )
    {
        // すでに計算済みの信号機の場合、その結果を返す
        if (cache.TryGetValue(signalName, out var signalIndication))
        {
            return signalIndication;
        }

        var result = SignalIndication.R;
        if (
            // 信号機が存在している
            signals.TryGetValue(signalName, out var signal)
            // Todo: 絶対信号機の場合、進路が空いている
            // 許容信号機の場合、対象軌道回路が短絡していない
            && !(signal.TrackCircuit?.TrackCircuitState.IsShortCircuit ?? false))
        {
            // 次の信号機名を取得
            var nextSignalNames = nextSignalDict.GetValueOrDefault(signalName, []);
            // 次の信号機の情報を取得
            var nextSignalIndications = nextSignalNames
                .Select(x => CalcSignalIndication(x, signals, nextSignalDict, cache))
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
}