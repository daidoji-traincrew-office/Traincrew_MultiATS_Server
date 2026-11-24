using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class SignalScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 250;

    private Dictionary<string, Phase> _oldSignalDataByName = [];

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        // 全状態の信号現示なので、1接続の詰まりが他ハブへ波及しないようlatest-onlyで送る。
        // 4ハブのどれかが通常配信だと、そのWhenAllが詰まって効果が出ないので全てlatest-onlyに揃える
        var trainSender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<TrainHub>>();
        var tidSender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<TIDHub>>();
        var commanderTableSender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<CommanderTableHub>>();
        var interlockingSender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<InterlockingHub>>();
        var signalService = scope.ServiceProvider.GetRequiredService<ISignalService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SignalScheduler>>();

        var signalData = await signalService.CalcAllSignalIndication();

        // 信号現示の変化をログ出力
        var changes = signalData
            .Select(signal => new
            {
                signal.Name,
                OldPhase = _oldSignalDataByName.GetValueOrDefault(signal.Name, Phase.None),
                Phase = signal.phase
            })
            .Where(x => x.OldPhase != x.Phase)
            .ToList();

        foreach (var change in changes)
        {
           logger.LogDebug("[{LogType}] 名前: {SignalName} 現示: {OldPhase} -> {Phase}",
               "信号現示変化", change.Name, change.OldPhase, change.Phase);
        }

        // 現在の信号データを保存
        _oldSignalDataByName = signalData.ToDictionary(s => s.Name, s => s.phase);

        await Task.WhenAll(
            trainSender.SendAllLatestAsync(nameof(ITrainClientContract.ReceiveSignalData), signalData),
            tidSender.SendAllLatestAsync(nameof(ITIDClientContract.ReceiveSignalData), signalData),
            commanderTableSender.SendAllLatestAsync(nameof(ICommanderTableClientContract.ReceiveSignalData), signalData),
            interlockingSender.SendAllLatestAsync(nameof(IInterlockingClientContract.ReceiveSignalData), signalData)
        );
    }
}