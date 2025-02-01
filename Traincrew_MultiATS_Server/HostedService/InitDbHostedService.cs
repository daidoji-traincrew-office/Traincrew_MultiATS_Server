using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.HostedService;

public class InitDbHostedService(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jsonstring = await File.ReadAllTextAsync("./Data/DBBase.json", cancellationToken);
        var DBBase = JsonSerializer.Deserialize<DBBasejson>(jsonstring);
        if (DBBase != null)
        {
            var initializer = new DbInitializer(DBBase, context, cancellationToken);
            await initializer.Initialize();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask; // 何もしない
    }
}

file class DbInitializer(DBBasejson DBBase, ApplicationDbContext context, CancellationToken cancellationToken)
{
    internal async Task Initialize()
    {
        await InitTrackCircuit();
        await InitSignalType();
        await InitSignal();
        await InitNextSignal();
        await InitTrackCircuitSignal(); // 追加
    }

    private async Task InitTrackCircuit()
    {
        var protectionZone = 0;
        foreach (var item in DBBase.trackCircuitList)
        {
            // Todo: ここでN+1問題が発生しているので、改善したほうが良いかも
            if (!context.TrackCircuits.Any(tc => tc.Name == item.Name))
            {
                context.TrackCircuits.Add(new TrackCircuit
                {
                    ProtectionZone = protectionZone,
                    Name = item.Name,
                    Type = ObjectType.TrackCircuit,
                    TrackCircuitState = new TrackCircuitState
                    {
                        IsShortCircuit = false,
                        TrainNumber = ""
                    }
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InitSignal()
    {
        // 信号情報登録
        foreach (var signalData in DBBase.signalDataList)
        {
            // 既に登録済みの場合、スキップ
            // Todo: ここでN+1問題が発生しているので、改善したほうが良いかも
            if (context.Signals.Any(s => s.Name == signalData.Name))
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
            await context.SaveChangesAsync(cancellationToken);
        }
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
                       if (context.NextSignals.Any(ns =>
                            ns.SignalName == signal.Name && ns.TargetSignalName == nextNextSignal))
                        {
                            continue;
                        }

                        nextNextSignals.Add(new()
                        {
                            SignalName = signal.Name,
                            SourceSignalName = nextSignal,
                            TargetSignalName = nextNextSignal,
                            Depth = depth
                        }); 
                    }
                }
            }
            context.NextSignals.AddRange(nextNextSignals); 
            await context.SaveChangesAsync(cancellationToken);
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