using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Activity;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

/// <summary>
/// 配信系6スケジューラ(Interlocking/Signal/TID/CTCP/CommanderTable/Train)を統合したスケジューラ。
/// 1ティックにつき1トランザクション/1スナップショットを <see cref="IBroadcastSnapshotService"/> から取得し、
/// 5ハブへ配信する。これにより軌道回路と信号現示が同一時点のデータになり、
/// 連動盤での描画の因果順逆転(ラインライト→信号ランプの順)が原理的に起きなくなる。
/// </summary>
public class BroadcastScheduler(IServiceScopeFactory serviceScopeFactory) : Scheduler(serviceScopeFactory)
{
    protected override int Interval => 250;

    /// <summary>
    /// 信号現示変化のログ出力用。SignalScheduler から移植。
    /// </summary>
    private Dictionary<string, Phase> _oldSignalDataByName = [];

    protected override async Task ExecuteTaskAsync(IServiceScope scope, System.Diagnostics.Activity? activity)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BroadcastScheduler>>();
        var snapshotService = scope.ServiceProvider.GetRequiredService<IBroadcastSnapshotService>();

        var snapshot = await snapshotService.BuildAsync();

        LogSignalChanges(snapshot.Signals, logger);

        var interlockingHubContext = scope.ServiceProvider
            .GetRequiredService<IHubContext<InterlockingHub, IInterlockingClientContract>>();
        var tidHubContext = scope.ServiceProvider
            .GetRequiredService<IHubContext<TIDHub, ITIDClientContract>>();
        var commanderTableHubContext = scope.ServiceProvider
            .GetRequiredService<IHubContext<CommanderTableHub, ICommanderTableClientContract>>();
        var ctcpHubContext = scope.ServiceProvider
            .GetRequiredService<IHubContext<CTCPHub, ICTCPClientContract>>();
        var trainHubContext = scope.ServiceProvider
            .GetRequiredService<IHubContext<TrainHub, ITrainClientContract>>();

        using (ActivitySources.Scheduler.StartActivity("Broadcast.Send"))
        {
            await Task.WhenAll(
                SendInterlockingAsync(interlockingHubContext, snapshot, logger),
                SendTidAsync(tidHubContext, snapshot, logger),
                SendCommanderTableAsync(commanderTableHubContext, snapshot, logger),
                SendCtcpAsync(ctcpHubContext, snapshot, logger),
                SendTrainAsync(trainHubContext, snapshot, logger));
        }
    }

    /// <summary>
    /// 連動盤への配信。ラインライト(ReceiveData)→信号ランプ(ReceiveSignalData)の描画順を保証するため
    /// 逐次 await する。ここを Task.WhenAll にすると位相がずれ、統合の意味が無くなる。
    /// </summary>
    private static async Task SendInterlockingAsync(
        IHubContext<InterlockingHub, IInterlockingClientContract> hubContext,
        BroadcastSnapshot snapshot,
        ILogger logger)
    {
        using var activity = ActivitySources.Scheduler.StartActivity("Broadcast.Send.Interlocking");
        try
        {
            activity?.SetTag("interlocking.phase", "data");
            await hubContext.Clients.All.ReceiveData(snapshot.Interlocking);
            activity?.SetTag("interlocking.phase", "signal");
            await hubContext.Clients.All.ReceiveSignalData(snapshot.Signals);
        }
        catch (System.Exception ex)
        {
            // 注意: ここで server_state.is_all_signal_relay_raised 等の連動装置側の状態を
            // 書き換えてはならない。配信の失敗は表示の失敗であって連動装置の失敗ではない。
            logger.LogError(ex, "連動盤への配信に失敗しました。");
        }
    }

    private static async Task SendTidAsync(
        IHubContext<TIDHub, ITIDClientContract> hubContext,
        BroadcastSnapshot snapshot,
        ILogger logger)
    {
        using var activity = ActivitySources.Scheduler.StartActivity("Broadcast.Send.Tid");
        try
        {
            await Task.WhenAll(
                hubContext.Clients.All.ReceiveData(snapshot.Tid),
                hubContext.Clients.All.ReceiveSignalData(snapshot.Signals));
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "TIDへの配信に失敗しました。");
        }
    }

    private static async Task SendCommanderTableAsync(
        IHubContext<CommanderTableHub, ICommanderTableClientContract> hubContext,
        BroadcastSnapshot snapshot,
        ILogger logger)
    {
        using var activity = ActivitySources.Scheduler.StartActivity("Broadcast.Send.CommanderTable");
        try
        {
            await Task.WhenAll(
                hubContext.Clients.All.ReceiveData(snapshot.CommanderTable),
                hubContext.Clients.All.ReceiveSignalData(snapshot.Signals));
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "司令卓への配信に失敗しました。");
        }
    }

    private static async Task SendCtcpAsync(
        IHubContext<CTCPHub, ICTCPClientContract> hubContext,
        BroadcastSnapshot snapshot,
        ILogger logger)
    {
        using var activity = ActivitySources.Scheduler.StartActivity("Broadcast.Send.Ctcp");
        try
        {
            // ICTCPClientContract に ReceiveSignalData は存在しない
            await hubContext.Clients.All.ReceiveData(snapshot.Ctcp);
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "CTCPへの配信に失敗しました。");
        }
    }

    private static async Task SendTrainAsync(
        IHubContext<TrainHub, ITrainClientContract> hubContext,
        BroadcastSnapshot snapshot,
        ILogger logger)
    {
        using var activity = ActivitySources.Scheduler.StartActivity("Broadcast.Send.Train");
        try
        {
            await Task.WhenAll(
                hubContext.Clients.All.ReceiveData(snapshot.Train),
                hubContext.Clients.All.ReceiveSignalData(snapshot.Signals));
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "ATSクライアントへの配信に失敗しました。");
        }
    }

    /// <summary>
    /// 信号現示の変化をログ出力する。SignalScheduler.cs から移植。
    /// </summary>
    private void LogSignalChanges(List<SignalData> signalData, ILogger logger)
    {
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
    }
}
