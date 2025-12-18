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
        var trainHubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TrainHub, ITrainClientContract>>();
        var tidHubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TIDHub, ITIDClientContract>>();
        var commanderTableHubContext = scope.ServiceProvider
            .GetRequiredService<IHubContext<CommanderTableHub, ICommanderTableClientContract>>();
        var interlockingHubContext = scope.ServiceProvider
            .GetRequiredService<IHubContext<InterlockingHub, IInterlockingClientContract>>();
        var signalService = scope.ServiceProvider.GetRequiredService<SignalService>();
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
           logger.LogDebug("[信号現示変化] 名前: {Name} 現示: {OldPhase} -> {Phase}", change.Name, change.OldPhase, change.Phase);
        }

        // 現在の信号データを保存
        _oldSignalDataByName = signalData.ToDictionary(s => s.Name, s => s.phase);

        await Task.WhenAll(
            trainHubContext.Clients.All.ReceiveSignalData(signalData),
            tidHubContext.Clients.All.ReceiveSignalData(signalData),
            commanderTableHubContext.Clients.All.ReceiveSignalData(signalData),
            interlockingHubContext.Clients.All.ReceiveSignalData(signalData)
        );
    }
}