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

        // 全状態のスナップショットなので、1接続の詰まりが他の接続・ハブへ波及しないようlatest-onlyで送る。
        // どれかのハブが通常配信だと、そのWhenAllが詰まって効果が出ないので全てlatest-onlyに揃える
        var interlockingSender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<InterlockingHub>>();
        var tidSender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<TIDHub>>();
        var commanderTableSender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<CommanderTableHub>>();
        var ctcpSender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<CTCPHub>>();
        var trainSender = scope.ServiceProvider.GetRequiredService<ILatestOnlySender<TrainHub>>();

        using (ActivitySources.Scheduler.StartActivity("Broadcast.Send"))
        {
            await Task.WhenAll(
                SendInterlockingAsync(interlockingSender, snapshot, logger),
                SendTidAsync(tidSender, snapshot, logger),
                SendCommanderTableAsync(commanderTableSender, snapshot, logger),
                SendCtcpAsync(ctcpSender, snapshot, logger),
                SendTrainAsync(trainSender, snapshot, logger));
        }
    }

    /// <summary>
    /// 連動盤への配信。ラインライト(ReceiveData)→信号ランプ(ReceiveSignalData)の順に、同じスナップショットの値をlatest-onlyで置く。
    /// Task.WhenAll にすると位相がずれ、統合の意味が無くなるので逐次 await する。
    /// </summary>
    /// <remarks>
    /// 既知の制約: 遅い接続では、mailboxが上書き時に送信待ちの位置を保つため、
    /// ReceiveSignalData が ReceiveData より先に届き得る(Dataだけ送信中でSignalが待ちのとき次ティックが来る場合)。
    /// 通常の速さの接続では起きない。人間判断で許容している。
    /// </remarks>
    internal static async Task SendInterlockingAsync(
        ILatestOnlySender<InterlockingHub> sender,
        BroadcastSnapshot snapshot,
        ILogger logger)
    {
        using var activity = ActivitySources.Scheduler.StartActivity("Broadcast.Send.Interlocking");
        try
        {
            await sender.SendAllLatestAsync(nameof(IInterlockingClientContract.ReceiveData), snapshot.Interlocking);
            await sender.SendAllLatestAsync(nameof(IInterlockingClientContract.ReceiveSignalData), snapshot.Signals);
        }
        catch (System.Exception ex)
        {
            // 注意: ここで server_state.is_all_signal_relay_raised 等の連動装置側の状態を
            // 書き換えてはならない。配信の失敗は表示の失敗であって連動装置の失敗ではない。
            logger.LogError(ex, "連動盤への配信に失敗しました。");
        }
    }

    internal static async Task SendTidAsync(
        ILatestOnlySender<TIDHub> sender,
        BroadcastSnapshot snapshot,
        ILogger logger)
    {
        using var activity = ActivitySources.Scheduler.StartActivity("Broadcast.Send.Tid");
        try
        {
            await Task.WhenAll(
                sender.SendAllLatestAsync(nameof(ITIDClientContract.ReceiveData), snapshot.Tid),
                sender.SendAllLatestAsync(nameof(ITIDClientContract.ReceiveSignalData), snapshot.Signals));
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "TIDへの配信に失敗しました。");
        }
    }

    internal static async Task SendCommanderTableAsync(
        ILatestOnlySender<CommanderTableHub> sender,
        BroadcastSnapshot snapshot,
        ILogger logger)
    {
        using var activity = ActivitySources.Scheduler.StartActivity("Broadcast.Send.CommanderTable");
        try
        {
            await Task.WhenAll(
                sender.SendAllLatestAsync(nameof(ICommanderTableClientContract.ReceiveData), snapshot.CommanderTable),
                sender.SendAllLatestAsync(nameof(ICommanderTableClientContract.ReceiveSignalData), snapshot.Signals));
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "司令卓への配信に失敗しました。");
        }
    }

    internal static async Task SendCtcpAsync(
        ILatestOnlySender<CTCPHub> sender,
        BroadcastSnapshot snapshot,
        ILogger logger)
    {
        using var activity = ActivitySources.Scheduler.StartActivity("Broadcast.Send.Ctcp");
        try
        {
            // ICTCPClientContract に ReceiveSignalData は存在しない
            await sender.SendAllLatestAsync(nameof(ICTCPClientContract.ReceiveData), snapshot.Ctcp);
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "CTCPへの配信に失敗しました。");
        }
    }

    internal static async Task SendTrainAsync(
        ILatestOnlySender<TrainHub> sender,
        BroadcastSnapshot snapshot,
        ILogger logger)
    {
        using var activity = ActivitySources.Scheduler.StartActivity("Broadcast.Send.Train");
        try
        {
            await Task.WhenAll(
                sender.SendAllLatestAsync(nameof(ITrainClientContract.ReceiveData), snapshot.Train),
                sender.SendAllLatestAsync(nameof(ITrainClientContract.ReceiveSignalData), snapshot.Signals));
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
